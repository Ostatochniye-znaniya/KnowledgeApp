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
    private readonly IDocxGeneratorService _docxGenerator;
    private readonly IFileStorage _storage;

    public GroupReportService(
        KnowledgeTestDbContext context,
        ICurrentUserProvider currentUser,
        IPdfGeneratorService pdfGenerator,
        IDocxGeneratorService docxGenerator,
        IFileStorage storage)
    {
        _context = context;
        _currentUser = currentUser;
        _pdfGenerator = pdfGenerator;
        _docxGenerator = docxGenerator;
        _storage = storage;
    }

    public async Task<byte[]> GeneratePdfPreview(
        int groupId,
        int semesterId,
        int disciplineId,
        CancellationToken cancellationToken)
    {
        var report = await GetReportAsync(groupId, semesterId, disciplineId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return _pdfGenerator.GenerateGroupReportPdf(report);
    }

    public async Task<byte[]> GenerateReport(
        int groupId,
        int semesterId,
        int disciplineId,
        CancellationToken cancellationToken)
    {
        var report = await GetReportAsync(groupId, semesterId, disciplineId, cancellationToken);
        var docx = _docxGenerator.GenerateGroupReportDocx(report);
        var storageKey = GetStorageKey(groupId, semesterId, disciplineId, "docx");
        await using var docxStream = new MemoryStream(docx, writable: false);
        await _storage.SaveAsync(
            storageKey,
            docxStream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            cancellationToken);

        return docx;
    }

    public async Task<byte[]> GenerateTemplate(
        int groupId,
        int semesterId,
        int disciplineId,
        CancellationToken cancellationToken)
    {
        var report = await GetReportAsync(groupId, semesterId, disciplineId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return _docxGenerator.GenerateGroupReportDocx(report);
    }

    public byte[] GenerateBlankReportTemplate() =>
        _docxGenerator.GenerateBlankReportDocx();

    public async Task<IReadOnlyList<GroupReportDto>> GetReportsAsync(
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

        var disciplines = await _context.Testings
            .AsNoTracking()
            .Where(t => t.GroupId == groupId && t.SemesterId == semesterId && t.DisciplineId != null)
            .Select(t => new { DisciplineId = t.DisciplineId!.Value, DisciplineName = t.Discipline!.Name })
            .Distinct()
            .ToListAsync(cancellationToken);

        var reports = disciplines
            .OrderBy(discipline => discipline.DisciplineName)
            .Select(discipline => new GroupReportDto
            {
                DisciplineId = discipline.DisciplineId,
                AcademicYear = AcademicPeriod.FormatAcademicYear(
                    semester.SemesterYear, semester.SemesterPart, semester.YearPart) ?? string.Empty,
                StudyProgramCode = group.StudyProgramCode ?? string.Empty,
                StudyProgramName = group.StudyProgramName,
                DisciplineName = discipline.DisciplineName,
                GroupName = group.GroupNumber,
                StudentNames = studentNames,
                DepartmentName = group.DepartmentName,
                FacultyName = group.FacultyName
            })
            .ToList();

        if (reports.Count == 0)
            throw new DocumentNotFoundException("Для группы нет дисциплин в выбранном семестре");

        return reports;
    }

    public async Task<DocumentFileDto> OpenStoredReport(
        int groupId,
        int semesterId,
        int disciplineId,
        CancellationToken cancellationToken)
    {
        await GetReportAsync(groupId, semesterId, disciplineId, cancellationToken);
        var storageKey = GetStorageKey(groupId, semesterId, disciplineId, "docx");
        var stream = await _storage.OpenReadAsync(storageKey, cancellationToken)
            ?? throw new DocumentNotFoundException("Сформированный DOCX ещё не сохранён");

        return new DocumentFileDto
        {
            Content = stream,
            FileName = GetDocxFileName(groupId, semesterId, disciplineId),
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
    }

    public static string GetDocxFileName(int groupId, int semesterId, int disciplineId) =>
        $"Отчёт_о_проверке_остаточных_знаний_группа_{groupId}_дисциплина_{disciplineId}_семестр_{semesterId}.docx";

    private async Task<GroupReportDto> GetReportAsync(
        int groupId,
        int semesterId,
        int disciplineId,
        CancellationToken cancellationToken)
    {
        var reports = await GetReportsAsync(groupId, semesterId, cancellationToken);
        return reports.SingleOrDefault(report => report.DisciplineId == disciplineId)
            ?? throw new DocumentNotFoundException("Дисциплина не найдена в выбранной группе и семестре");
    }

    private static string GetStorageKey(int groupId, int semesterId, int disciplineId, string extension) =>
        $"group-reports/{groupId}/{semesterId}/{disciplineId}/report.{extension}";
}
