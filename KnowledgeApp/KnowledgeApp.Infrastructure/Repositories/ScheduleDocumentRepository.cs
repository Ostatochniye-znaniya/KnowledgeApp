using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

public class ScheduleDocumentRepository
{
    private readonly KnowledgeTestDbContext _context;

    public ScheduleDocumentRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScheduleDocumentModel>> GetByFaculty(int facultyId, int? semesterId, DocumentStatus? status)
    {
        var query = _context.ScheduleDocuments.AsNoTracking().Where(d => d.FacultyId == facultyId);

        if (semesterId.HasValue)
            query = query.Where(d => d.SemesterId == semesterId.Value);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        return await Project(query)
            .OrderByDescending(d => d.SemesterYear)
            .ThenByDescending(d => d.SemesterPart)
            .ToListAsync();
    }

    public async Task<ScheduleDocumentModel?> GetById(int id)
    {
        return await Project(_context.ScheduleDocuments.AsNoTracking().Where(d => d.Id == id))
            .SingleOrDefaultAsync();
    }

    public async Task<ScheduleDocumentModel?> GetByFacultyAndSemester(int facultyId, int semesterId)
    {
        return await Project(_context.ScheduleDocuments.AsNoTracking()
                .Where(d => d.FacultyId == facultyId && d.SemesterId == semesterId))
            .SingleOrDefaultAsync();
    }

    public Task<bool> FacultyExists(int facultyId) =>
        _context.Faculties.AnyAsync(f => f.Id == facultyId);

    public Task<bool> SemesterExists(int semesterId) =>
        _context.Semesters.AnyAsync(s => s.Id == semesterId);

    public async Task<(bool Saved, string? PreviousKey)> Upsert(int facultyId, int semesterId, string fileKey,
        string fileName, long fileSize, DateTime uploadedAt, int? uploadedByUserId)
    {
        var current = await _context.ScheduleDocuments.AsNoTracking()
            .Where(d => d.FacultyId == facultyId && d.SemesterId == semesterId)
            .Select(d => new { d.Id, d.FileKey, d.Status })
            .SingleOrDefaultAsync();

        if (current == null)
        {
            _context.ScheduleDocuments.Add(new ScheduleDocument
            {
                FacultyId = facultyId,
                SemesterId = semesterId,
                FileKey = fileKey,
                FileName = fileName,
                FileSize = fileSize,
                UploadedAt = uploadedAt,
                UploadedByUserId = uploadedByUserId,
                Status = DocumentStatus.Pending
            });

            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateException)
            {
                // параллельная первая загрузка уже создала строку
                _context.ChangeTracker.Clear();
                return (false, null);
            }
        }

        if (current.Status == DocumentStatus.Signed)
            return (false, null);

        var affected = await _context.ScheduleDocuments
            .Where(d => d.Id == current.Id && d.Status != DocumentStatus.Signed && d.FileKey == current.FileKey)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.FileKey, fileKey)
                .SetProperty(d => d.FileName, fileName)
                .SetProperty(d => d.FileSize, fileSize)
                .SetProperty(d => d.UploadedAt, uploadedAt)
                .SetProperty(d => d.UploadedByUserId, uploadedByUserId)
                .SetProperty(d => d.Status, DocumentStatus.Pending)
                .SetProperty(d => d.ReviewComment, (string?)null)
                .SetProperty(d => d.ReviewedAt, (DateTime?)null)
                .SetProperty(d => d.ReviewedByUserId, (int?)null));

        return affected > 0 ? (true, current.FileKey) : (false, null);
    }

    public async Task<bool> Review(int id, DocumentStatus newStatus, string? comment, DateTime reviewedAt, int? reviewerId)
    {
        var affected = await _context.ScheduleDocuments
            .Where(d => d.Id == id && d.Status == DocumentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Status, newStatus)
                .SetProperty(d => d.ReviewComment, comment)
                .SetProperty(d => d.ReviewedAt, reviewedAt)
                .SetProperty(d => d.ReviewedByUserId, reviewerId));

        return affected > 0;
    }

    private static IQueryable<ScheduleDocumentModel> Project(IQueryable<ScheduleDocument> documents) =>
        documents.Select(d => new ScheduleDocumentModel
        {
            Id = d.Id,
            FacultyId = d.FacultyId,
            SemesterId = d.SemesterId,
            SemesterYear = d.Semester != null ? d.Semester.SemesterYear : 0,
            SemesterPart = d.Semester != null ? d.Semester.SemesterPart : 0,
            FileKey = d.FileKey,
            FileName = d.FileName,
            FileSize = d.FileSize,
            UploadedAt = d.UploadedAt,
            Status = d.Status,
            ReviewComment = d.ReviewComment,
            ReviewedAt = d.ReviewedAt
        });
}
