using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using KnowledgeApp.Domain.Planning;

namespace KnowledgeApp.Infrastructure.Repositories;

/// <summary>
/// Выбор дисциплин для групп (ТЗ, п. 2.4).
/// Тестирования по дисциплинам создаются при сохранении выбора для подтверждённой группы.
/// </summary>
public class GroupDisciplineRepository
{
    private readonly KnowledgeTestDbContext _context;

    public GroupDisciplineRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlanningSemester>> GetCalendarAsync()
    {
        return await _context.Semesters
            .AsNoTracking()
            .Select(s => new PlanningSemester(s.Id, s.SemesterYear, s.SemesterPart)).ToListAsync();
    }

    public async Task WithPlanLock(Func<Task> action) =>
        await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var tx = await _context.Database.BeginTransactionAsync();
            var state = await _context.LockTestingPlan();
            await action();
            state.Version++;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        });

    private IQueryable<StudyGroup> SelectedGroups(int semesterId) => _context.StudyGroups.Where(g =>
        _context.GroupParticipationDecisions.Any(d => d.SemesterId == semesterId && d.StudyProgramId == g.StudyProgramId
            && d.SelectedGroupId == g.Id)
        || (!_context.GroupParticipationDecisions.Any(d => d.SemesterId == semesterId && d.StudyProgramId == g.StudyProgramId)
            && g.Testings.Any(t => t.SemesterId == semesterId && (t.DisciplineId != null || t.ReportId != null || t.ScheduledDate != null))));

    public Task<bool> IsSelectedAsync(int groupId, int semesterId) =>
        SelectedGroups(semesterId).AnyAsync(g => g.Id == groupId);

    public Task<bool> SemesterExistsAsync(int semesterId)
    {
        return _context.Semesters.AnyAsync(s => s.Id == semesterId);
    }

    /// <summary>Название кафедры; null — если кафедры нет.</summary>
    public Task<string?> GetDepartmentNameAsync(int departmentId)
    {
        return _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == departmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Группы кафедры, подтверждённые HOD, а также уже начатые старые процессы,
    /// с уже выбранными дисциплинами.
    /// </summary>
    public async Task<List<GroupDisciplinesModel>> GetGroupsAsync(int semesterId, int departmentId)
    {
        var semester = await _context.Semesters.AsNoTracking().SingleAsync(s => s.Id == semesterId);
        var lockedIds = await GetLockedTestingIdsAsync(semesterId);
        var rows = await SelectedGroups(semesterId)
            .AsNoTracking()
            .Where(g => g.StudyProgram!.DepartmentId == departmentId)
            .OrderBy(g => g.GroupNumber)
            .Select(g => new
            {
                g.Id,
                g.GroupNumber,
                g.StudyProgramId,
                StudyProgramName = g.StudyProgram!.Name,
                DirectionCode = g.StudyProgram.CypherOfTheDirection,
                AvailableDisciplinesCount = g.StudyProgram.StudyProgramDisciplines.Count,
                DepartmentId = g.StudyProgram!.DepartmentId,
                DepartmentName = g.StudyProgram!.Department!.Name,
                FacultyName = g.StudyProgram!.Department!.Faculty!.FacultyName,
                Disciplines = g.Testings
                    .Where(t => t.SemesterId == semesterId && t.DisciplineId != null)
                    .OrderBy(t => t.Id)
                    .Select(t => new {
                        t.Discipline!.Id, t.Discipline.Name, t.Discipline.DepartmentId,
                        DepartmentName = t.Discipline.Department != null ? t.Discipline.Department.Name : null,
                        FacultyId = t.Discipline.Department != null ? t.Discipline.Department.FacultyId : null,
                        FacultyName = t.Discipline.Department != null && t.Discipline.Department.Faculty != null
                            ? t.Discipline.Department.Faculty.FacultyName : null,
                        IsInStudyProgram = t.Discipline.StudyProgramDisciplines.Any(p => p.StudyProgramId == g.StudyProgramId),
                        Locked = lockedIds.Contains(t.Id)
                    })
                    .ToList()
            })
            .ToListAsync();

        return rows.Select(r => new GroupDisciplinesModel
        {
            GroupId = r.Id,
            GroupNumber = r.GroupNumber,
            StudyProgramId = r.StudyProgramId,
            StudyProgramName = r.StudyProgramName,
            DirectionCode = r.DirectionCode,
            AvailableDisciplinesCount = r.AvailableDisciplinesCount,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.DepartmentName,
            FacultyName = r.FacultyName,
            SemesterId = semesterId,
            SemesterYear = semester.SemesterYear,
            SemesterPart = semester.SemesterPart,
            Disciplines = r.Disciplines
                .GroupBy(d => d.Id)
                .Select(ds => {
                    var d = ds.First();
                    return new DisciplineModel(d.Id, d.Name, d.DepartmentId) {
                        DepartmentName = d.DepartmentName, FacultyId = d.FacultyId, FacultyName = d.FacultyName,
                        IsInStudyProgram = d.IsInStudyProgram, Locked = ds.Any(item => item.Locked)
                    };
                })
                .ToList()
        }).ToList();
    }

    /// <summary>
    /// Кафедра, за которой закреплена образовательная программа группы.
    /// Found == false — если группы нет; DepartmentId == null — если у программы нет кафедры.
    /// </summary>
    public async Task<(bool Found, int? DepartmentId)> GetGroupDepartmentAsync(int groupId)
    {
        var group = await _context.StudyGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { DepartmentId = g.StudyProgram!.DepartmentId })
            .SingleOrDefaultAsync();

        return group == null ? (false, null) : (true, group.DepartmentId);
    }

    /// <summary>Дисциплины программы группы, включая дисциплины других кафедр и факультетов.</summary>
    public async Task<List<DisciplineModel>> GetStudyProgramDisciplinesAsync(int groupId)
    {
        var disciplines = await _context.Disciplines
            .AsNoTracking()
            .Where(d => d.StudyProgramDisciplines.Any(p =>
                _context.StudyGroups.Any(g => g.Id == groupId && g.StudyProgramId == p.StudyProgramId)))
            .OrderBy(d => d.Name)
            .Select(d => new {
                d.Id, d.Name, d.DepartmentId,
                DepartmentName = d.Department != null ? d.Department.Name : null,
                FacultyId = d.Department != null ? d.Department.FacultyId : null,
                FacultyName = d.Department != null && d.Department.Faculty != null ? d.Department.Faculty.FacultyName : null
            })
            .ToListAsync();

        return disciplines
            .Select(d => new DisciplineModel(d.Id, d.Name, d.DepartmentId) {
                DepartmentName = d.DepartmentName, FacultyId = d.FacultyId, FacultyName = d.FacultyName
            })
            .ToList();
    }

    // Смена дисциплины не должна оставлять назначения преподавателей и отчёты от прежней дисциплины.
    public Task<List<int>> GetLockedTestingIdsAsync(int semesterId, int? groupId = null) =>
        _context.Testings.Where(t => t.SemesterId == semesterId && (!groupId.HasValue || t.GroupId == groupId))
            .Where(t => t.ReportId != null || t.ScheduledDate != null || t.ScheduledTime != null
                || t.SelectionPeriodStart != null || t.SelectionPeriodEnd != null || t.ExternalRequestSentAt != null
                || _context.TestingTeacherApprovals.Any(a => a.TestingId == t.Id))
            .Select(t => t.Id).ToListAsync();

    /// <summary>Тестирования группы в семестре (с отслеживанием изменений), по порядку создания.</summary>
    public Task<List<Testing>> GetGroupTestingsAsync(int groupId, int semesterId)
    {
        return _context.Testings
            .Where(t => t.GroupId == groupId && t.SemesterId == semesterId)
            .OrderBy(t => t.Id)
            .ToListAsync();
    }

    public async Task AddTestingsAsync(int groupId, int semesterId, IEnumerable<int> disciplineIds)
    {
        var decisionId = await _context.GroupParticipationDecisions
            .Where(d => d.SemesterId == semesterId && d.SelectedGroupId == groupId)
            .Select(d => (int?)d.Id).SingleOrDefaultAsync();
        var recommendationId = await _context.RecommendationHistory
            .Where(r => r.SemesterId == semesterId && r.StudyGroupId == groupId)
            .Select(r => (int?)r.Id).SingleOrDefaultAsync();

        _context.Testings.AddRange(disciplineIds.Select(disciplineId => new Testing
        {
            GroupId = groupId,
            SemesterId = semesterId,
            DisciplineId = disciplineId,
            GroupParticipationDecisionId = decisionId,
            RecommendationHistoryId = recommendationId
        }));
    }
}
