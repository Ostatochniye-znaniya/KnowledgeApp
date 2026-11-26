using KnowledgeApp.Application.DTOs.Statistics;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class StatisticsService
{
    private readonly KnowledgeTestDbContext _context;
    private readonly ICurrentUserProvider _currentUser;

    public StatisticsService(KnowledgeTestDbContext context, ICurrentUserProvider currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<StatisticsFilterOptionsDto> GetFilters(CancellationToken cancellationToken)
    {
        var semesters = await _context.Semesters.AsNoTracking()
            .OrderByDescending(s => s.SemesterYear)
            .ThenByDescending(s => s.SemesterPart)
            .ToListAsync(cancellationToken);

        return new StatisticsFilterOptionsDto
        {
            Semesters = semesters.Select(s => new StatisticsSemesterOptionDto
            {
                Id = s.Id,
                Label = AcademicPeriod.FormatSemester(s.SemesterYear, s.SemesterPart),
                IsSpring = IsSpring(s)
            }).ToList()
        };
    }

    public async Task<StatisticsSummaryDto> GetSummary(
        int? semesterId,
        int? facultyId,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        var semester = await ResolveSemester(semesterId, cancellationToken);
        var isSpring = IsSpring(semester);
        var order = await ResolveOrder(semester.Id, cancellationToken);

        facultyId ??= _currentUser.FacultyId;
        departmentId ??= _currentUser.DepartmentId;

        var departmentsQuery = _context.Departments.AsNoTracking()
            .Include(d => d.Faculty)
            .Include(d => d.StudyPrograms)
            .AsQueryable();

        if (facultyId is > 0)
            departmentsQuery = departmentsQuery.Where(d => d.FacultyId == facultyId);
        if (departmentId is > 0)
            departmentsQuery = departmentsQuery.Where(d => d.Id == departmentId);

        var departments = await departmentsQuery
            .OrderBy(d => d.Faculty!.FacultyName)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        var departmentIds = departments.Select(d => d.Id).ToList();

        var testings = await _context.Testings.AsNoTracking()
            .Include(t => t.Report)
            .Include(t => t.Group)!.ThenInclude(g => g!.StudyProgram)
            .Where(t => t.SemesterId == semester.Id
                        && t.Group != null
                        && t.Group.StudyProgram != null
                        && t.Group.StudyProgram.DepartmentId != null
                        && departmentIds.Contains(t.Group.StudyProgram.DepartmentId.Value))
            .ToListAsync(cancellationToken);

        var programDisciplineCounts = await _context.StudyProgramDisciplines.AsNoTracking()
            .Include(spd => spd.StudyProgram)
            .Where(spd => spd.StudyProgram.DepartmentId != null
                          && departmentIds.Contains(spd.StudyProgram.DepartmentId.Value))
            .GroupBy(spd => spd.StudyProgram.DepartmentId!.Value)
            .Select(g => new { DepartmentId = g.Key, Count = g.Select(x => x.DisciplineId).Distinct().Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, cancellationToken);

        var faculties = departments
            .GroupBy(d => d.FacultyId ?? 0)
            .Select(g =>
            {
                var faculty = g.First().Faculty;
                var facultyIdValue = faculty?.Id ?? g.Key;
                var facultyName = faculty?.FacultyName ?? "Без факультета";
                return new FacultyStatisticsDto
                {
                    Id = facultyIdValue,
                    Name = facultyName,
                    Code = StatisticsCalculations.ShortCode(facultyName),
                    Departments = g.Select(dept => BuildDepartment(dept, testings, programDisciplineCounts, isSpring)).ToList()
                };
            })
            .OrderBy(f => f.Name)
            .ToList();

        return new StatisticsSummaryDto
        {
            SemesterId = semester.Id,
            SemesterLabel = AcademicPeriod.FormatSemester(semester.SemesterYear, semester.SemesterPart),
            IsSpring = isSpring,
            OrderId = order?.Id,
            RequiresPaperReports = order?.RequiresPaperReports ?? true,
            Faculties = faculties
        };
    }

    public async Task<DepartmentStatisticsDetailsDto> GetDepartmentDetails(
        int? semesterId,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        departmentId ??= _currentUser.DepartmentId;
        if (departmentId is not > 0)
            throw new DocumentValidationException("Укажите кафедру.");

        var semester = await ResolveSemester(semesterId, cancellationToken);
        var order = await ResolveOrder(semester.Id, cancellationToken);

        var department = await _context.Departments.AsNoTracking()
            .Include(d => d.Faculty)
            .FirstOrDefaultAsync(d => d.Id == departmentId, cancellationToken)
            ?? throw new DocumentNotFoundException("Кафедра не найдена.");

        var testings = await _context.Testings.AsNoTracking()
            .Include(t => t.Report)
            .Include(t => t.Discipline)!.ThenInclude(d => d!.DisciplineTeachers)!.ThenInclude(dt => dt.ResponsibleTeacher)
            .Include(t => t.Group)!.ThenInclude(g => g!.StudyProgram)
            .Where(t => t.SemesterId == semester.Id
                        && t.Group != null
                        && t.Group.StudyProgram != null
                        && t.Group.StudyProgram.DepartmentId == departmentId)
            .ToListAsync(cancellationToken);

        var disciplines = testings
            .Where(t => t.DisciplineId != null && t.Discipline != null)
            .GroupBy(t => t.DisciplineId!.Value)
            .Select(g =>
            {
                var discipline = g.First().Discipline!;
                var groups = g
                    .GroupBy(t => t.GroupId)
                    .Select(gg =>
                    {
                        var testing = gg.First();
                        var report = testing.Report;
                        var eStatus = StatisticsCalculations.MapElectronicStatus(report);
                        var pStatus = StatisticsCalculations.MapPaperStatus(report);
                        return new DisciplineGroupStatisticsDto
                        {
                            GroupNumber = testing.Group?.GroupNumber ?? $"#{testing.GroupId}",
                            Course = StatisticsCalculations.GuessCourse(testing.Group?.GroupNumber),
                            Teacher = TeacherName(discipline),
                            EReportStatus = eStatus,
                            PReportStatus = pStatus,
                            AllPassed = eStatus == "done" && pStatus == "done"
                        };
                    })
                    .OrderBy(x => x.GroupNumber)
                    .ToList();

                var eDone = groups.Count(x => x.EReportStatus == "done");
                var eRemarks = groups.Count(x => x.EReportStatus == "remarks");
                var pDone = groups.Count(x => x.PReportStatus == "done");
                var pRemarks = groups.Count(x => x.PReportStatus == "remarks");

                return new DisciplineStatisticsDto
                {
                    Id = discipline.Id,
                    Title = discipline.Name,
                    Course = groups.FirstOrDefault()?.Course ?? 0,
                    Semester = AcademicPeriod.FormatSemester(semester.SemesterYear, semester.SemesterPart),
                    ProfilesCount = groups.Select(x => x.GroupNumber).Distinct().Count(),
                    GroupFact = groups.Count,
                    ClassNorm = groups.Count,
                    ClassFact = groups.Count,
                    EReportDone = eDone,
                    EReportRemarks = eRemarks,
                    PReportDone = pDone,
                    PReportRemarks = pRemarks,
                    Groups = groups
                };
            })
            .OrderBy(d => d.Title)
            .ToList();

        return new DepartmentStatisticsDetailsDto
        {
            DepartmentId = department.Id,
            DepartmentName = department.Name,
            FacultyCode = StatisticsCalculations.ShortCode(department.Faculty?.FacultyName ?? ""),
            SemesterId = semester.Id,
            RequiresPaperReports = order?.RequiresPaperReports ?? true,
            Disciplines = disciplines
        };
    }

    private static DepartmentStatisticsDto BuildDepartment(
        Department dept,
        List<Testing> allTestings,
        Dictionary<int, int> programDisciplineCounts,
        bool isSpring)
    {
        var programs = dept.StudyPrograms?.ToList() ?? [];
        var profilesCount = programs.Count;
        var magistracyCount = programs.Count(p =>
            (p.Name?.Contains("магистр", StringComparison.OrdinalIgnoreCase) ?? false)
            || (p.CypherOfTheDirection?.Contains(".04.", StringComparison.Ordinal) ?? false));
        var bachelorProfiles = Math.Max(0, profilesCount - magistracyCount);
        var coursesCount = bachelorProfiles > 0 ? 3 : 0;

        var deptTestings = allTestings
            .Where(t => t.Group?.StudyProgram?.DepartmentId == dept.Id)
            .ToList();

        var groupFact = deptTestings.Select(t => t.GroupId).Distinct().Count();
        var classFact = deptTestings.Count;
        var classNorm = programDisciplineCounts.TryGetValue(dept.Id, out var planned)
            ? Math.Max(planned, classFact)
            : classFact;

        var reports = deptTestings.Select(t => t.Report).Where(r => r != null).Cast<Report>().ToList();
        var eDone = reports.Count(r => r.DoneInElectronicForm == true || r.Status == DocumentStatus.Signed);
        var eRemarks = reports.Count(r => r.Status == DocumentStatus.Rejected);
        var pDone = reports.Count(r => r.DoneInPaperForm == true);
        var pRemarks = reports.Count(r =>
            r.DoneInPaperForm == false
            && r.Status == DocumentStatus.Rejected
            && !string.IsNullOrWhiteSpace(r.ReviewComment));

        return new DepartmentStatisticsDto
        {
            Id = dept.Id,
            Name = dept.Name,
            ProfilesCount = profilesCount,
            CoursesCount = coursesCount,
            MagistracyCount = magistracyCount,
            GroupNorm = StatisticsCalculations.CalculateGroupNorm(bachelorProfiles, coursesCount, isSpring, magistracyCount),
            GroupFact = groupFact,
            ClassNorm = classNorm,
            ClassFact = classFact,
            EReportDone = eDone,
            EReportRemarks = eRemarks,
            PReportDone = pDone,
            PReportRemarks = pRemarks
        };
    }

    private async Task<Semester> ResolveSemester(int? semesterId, CancellationToken cancellationToken)
    {
        if (semesterId is > 0)
        {
            return await _context.Semesters.AsNoTracking()
                       .FirstOrDefaultAsync(s => s.Id == semesterId, cancellationToken)
                   ?? throw new DocumentNotFoundException("Семестр не найден.");
        }

        var latest = await _context.Semesters.AsNoTracking()
            .OrderByDescending(s => s.SemesterYear)
            .ThenByDescending(s => s.SemesterPart)
            .FirstOrDefaultAsync(cancellationToken);

        return latest ?? throw new DocumentNotFoundException("В системе нет семестров.");
    }

    private async Task<Order?> ResolveOrder(int semesterId, CancellationToken cancellationToken)
    {
        return await _context.Orders.AsNoTracking()
            .Where(o => o.SemesterId == semesterId)
            .OrderByDescending(o => o.Status == "issued")
            .ThenByDescending(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsSpring(Semester semester) =>
        semester.YearPart == 2 || ((semester.YearPart is 0) && semester.SemesterPart == 1) || semester.SemesterPart == 1;

    private static string TeacherName(Discipline discipline)
    {
        var teacher = discipline.DisciplineTeachers?
            .Select(dt => dt.ResponsibleTeacher?.Name)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        return teacher ?? "—";
    }
}
