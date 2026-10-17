using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

public class ReportDocumentRepository
{
    private readonly KnowledgeTestDbContext _context;

    public ReportDocumentRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReportDocumentModel>> GetSubmittedByFaculty(int facultyId, int? departmentId, DocumentStatus? status)
    {
        var query = _context.Reports.AsNoTracking()
            .Where(r => r.Status != null && r.FilePath != null)
            .Where(r => r.Discipline != null && r.Discipline.Department != null
                        && r.Discipline.Department.FacultyId == facultyId);

        if (departmentId.HasValue)
            query = query.Where(r => r.Discipline!.DepartmentId == departmentId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await LoadModels(query);
    }

    public async Task<ReportDocumentModel?> GetById(int reportId)
    {
        var models = await LoadModels(_context.Reports.AsNoTracking().Where(r => r.Id == reportId));
        return models.FirstOrDefault();
    }

    public async Task<(bool Replaced, string? PreviousKey)> AttachFile(int reportId, string fileKey, string fileName, DateTime uploadedAt)
    {
        var current = await _context.Reports.AsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => new { r.FilePath, r.Status })
            .SingleOrDefaultAsync();

        if (current == null || current.Status == DocumentStatus.Signed)
            return (false, null);

        var previousKey = current.FilePath;
        var affected = await _context.Reports
            .Where(r => r.Id == reportId
                        && (r.Status == null || r.Status != DocumentStatus.Signed)
                        && r.FilePath == previousKey)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.FilePath, fileKey)
                .SetProperty(r => r.FileName, fileName)
                .SetProperty(r => r.UploadedAt, uploadedAt)
                .SetProperty(r => r.Status, DocumentStatus.Pending)
                .SetProperty(r => r.ReviewComment, (string?)null)
                .SetProperty(r => r.ReviewedAt, (DateTime?)null)
                .SetProperty(r => r.ReviewedByUserId, (int?)null));

        return affected > 0 ? (true, previousKey) : (false, null);
    }

    public async Task<bool> Review(int reportId, DocumentStatus newStatus, string? comment, DateTime reviewedAt, int? reviewerId)
    {
        var affected = await _context.Reports
            .Where(r => r.Id == reportId && r.Status == DocumentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, newStatus)
                .SetProperty(r => r.ReviewComment, comment)
                .SetProperty(r => r.ReviewedAt, reviewedAt)
                .SetProperty(r => r.ReviewedByUserId, reviewerId));

        return affected > 0;
    }

    private async Task<List<ReportDocumentModel>> LoadModels(IQueryable<Report> reports)
    {
        var rows = await reports
            .Select(r => new ReportDocumentModel
            {
                ReportId = r.Id,
                FacultyId = r.Discipline != null && r.Discipline.Department != null
                    ? r.Discipline.Department.FacultyId
                    : null,
                DepartmentId = r.Discipline != null ? r.Discipline.DepartmentId : null,
                DepartmentName = r.Discipline != null && r.Discipline.Department != null
                    ? r.Discipline.Department.Name
                    : null,
                DisciplineName = r.Discipline != null ? r.Discipline.Name : null,
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher != null ? r.Teacher.Name : null,
                FileKey = r.FilePath,
                FileName = r.FileName,
                UploadedAt = r.UploadedAt,
                Status = r.Status,
                ReviewComment = r.ReviewComment,
                ReviewedAt = r.ReviewedAt
            })
            .ToListAsync();

        if (rows.Count == 0) return rows;

        // навигация Testing.Report смаплена на semester_id, поэтому join по report_id руками
        var reportIds = rows.Select(r => r.ReportId).ToList();
        var testings = await (
                from t in _context.Testings.AsNoTracking()
                where t.ReportId != null && reportIds.Contains(t.ReportId.Value)
                join s in _context.Semesters.AsNoTracking() on t.SemesterId equals (int?)s.Id into semesters
                from s in semesters.DefaultIfEmpty()
                select new
                {
                    t.Id,
                    ReportId = t.ReportId!.Value,
                    GroupNumber = t.Group != null ? t.Group.GroupNumber : null,
                    StudyProgramCode = t.Group != null && t.Group.StudyProgram != null
                        ? t.Group.StudyProgram.CypherOfTheDirection
                        : null,
                    SemesterYear = s != null ? (int?)s.SemesterYear : null,
                    SemesterPart = s != null ? (int?)s.SemesterPart : null,
                    YearPart = s != null ? (int?)s.YearPart : null
                })
            .ToListAsync();

        var firstTestingByReport = testings
            .GroupBy(t => t.ReportId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Id).First());

        foreach (var row in rows)
        {
            if (!firstTestingByReport.TryGetValue(row.ReportId, out var testing)) continue;

            row.GroupNumber = testing.GroupNumber;
            row.StudyProgramCode = testing.StudyProgramCode;
            row.SemesterYear = testing.SemesterYear;
            row.SemesterPart = testing.SemesterPart;
            row.YearPart = testing.YearPart;
        }

        return rows;
    }
}
