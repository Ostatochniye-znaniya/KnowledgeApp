using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public class GroupReportService
{
    private readonly KnowledgeTestDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly IFileStorage _storage;

    public GroupReportService(
        KnowledgeTestDbContext context,
        ICurrentUserProvider currentUser,
        IPdfGeneratorService pdfGenerator,
        IFileStorage storage)
    {
        _context = context;
        _currentUser = currentUser;
        _pdfGenerator = pdfGenerator;
        _storage = storage;
    }

    public async Task<byte[]> GenerateReport(int groupId, int semesterId, CancellationToken cancellationToken)
    {
        var pages = await GetReportPagesAsync(groupId, semesterId, cancellationToken);
        var pdf = _pdfGenerator.GenerateGroupReportPdf(pages);
        var storageKey = GetStorageKey(groupId, semesterId);
        await using var pdfStream = new MemoryStream(pdf, writable: false);
        await _storage.SaveAsync(storageKey, pdfStream, "application/pdf", cancellationToken);

        return pdf;
    }

    public async Task<IReadOnlyList<GroupReportPageDto>> GetReportPagesAsync(
        int groupId,
        int semesterId,
        CancellationToken cancellationToken)
    {
        var group = await _context.StudyGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new
            {
                g.GroupNumber,
                StudyProgramCode = g.StudyProgram!.CypherOfTheDirection,
                StudyProgramName = g.StudyProgram!.Name,
                DepartmentName = g.StudyProgram!.Department!.Name,
                FacultyId = g.StudyProgram!.Department!.FacultyId,
                FacultyName = g.StudyProgram!.Department!.Faculty!.FacultyName
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (group == null || group.FacultyId != _currentUser.FacultyId)
            throw new DocumentNotFoundException("Группа не найдена");

        var semester = await _context.Semesters
            .AsNoTracking()
            .Where(s => s.Id == semesterId)
            .Select(s => new { s.SemesterYear, s.SemesterPart, s.YearPart })
            .SingleOrDefaultAsync(cancellationToken);

        if (semester == null)
            throw new DocumentNotFoundException("Семестр не найден");

        var studentNames = await _context.Students
            .AsNoTracking()
            .Where(s => s.GroupId == groupId && s.User.Name != null && s.User.Name.Trim() != "")
            .OrderBy(s => s.User.Name)
            .Select(s => s.User.Name!.Trim())
            .ToListAsync(cancellationToken);

        var teacherRows = await _context.Testings
            .AsNoTracking()
            .Where(t => t.GroupId == groupId && t.SemesterId == semesterId && t.DisciplineId != null)
            .SelectMany(
                t => t.Discipline!.DisciplineTeachers.DefaultIfEmpty(),
                (t, teacher) => new
                {
                    DisciplineId = t.DisciplineId!.Value,
                    DisciplineName = t.Discipline!.Name,
                    TeacherName = teacher != null && teacher.ResponsibleTeacher != null
                        ? teacher.ResponsibleTeacher.Name
                        : null
                })
            .ToListAsync(cancellationToken);

        var pages = teacherRows
            .GroupBy(row => row.DisciplineId)
            .OrderBy(groupRows => groupRows.First().DisciplineName)
            .Select(groupRows => new GroupReportPageDto
            {
                AcademicYear = AcademicPeriod.FormatAcademicYear(
                    semester.SemesterYear, semester.SemesterPart, semester.YearPart) ?? string.Empty,
                StudyProgramCode = group.StudyProgramCode ?? string.Empty,
                StudyProgramName = group.StudyProgramName,
                DisciplineName = groupRows.First().DisciplineName,
                TeacherNames = string.Join(", ", groupRows
                    .Select(row => row.TeacherName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.CurrentCulture)),
                GroupName = group.GroupNumber,
                StudentNames = studentNames,
                DepartmentName = group.DepartmentName,
                FacultyName = group.FacultyName
            })
            .ToList();

        if (pages.Count == 0)
            throw new DocumentNotFoundException("Для группы нет дисциплин в выбранном семестре");

        return pages;
    }

    public async Task<DocumentFileDto> OpenStoredReport(
        int groupId,
        int semesterId,
        CancellationToken cancellationToken)
    {
        var facultyId = await _context.StudyGroups
            .AsNoTracking()
            .Where(group => group.Id == groupId)
            .Select(group => (int?)group.StudyProgram!.Department!.FacultyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (facultyId == null || facultyId != _currentUser.FacultyId)
            throw new DocumentNotFoundException("Группа не найдена");

        var semesterExists = await _context.Semesters
            .AsNoTracking()
            .AnyAsync(semester => semester.Id == semesterId, cancellationToken);

        if (!semesterExists)
            throw new DocumentNotFoundException("Семестр не найден");

        var storageKey = GetStorageKey(groupId, semesterId);
        var stream = await _storage.OpenReadAsync(storageKey, cancellationToken)
            ?? throw new DocumentNotFoundException("Сформированный PDF ещё не сохранён");

        return new DocumentFileDto
        {
            Content = stream,
            FileName = GetFileName(groupId, semesterId)
        };
    }

    public static string GetFileName(int groupId, int semesterId) =>
        $"Отчёт_о_проверке_остаточных_знаний_группа_{groupId}_семестр_{semesterId}.pdf";

    private static string GetStorageKey(int groupId, int semesterId) =>
        $"group-reports/{groupId}/{semesterId}/report.pdf";
}
