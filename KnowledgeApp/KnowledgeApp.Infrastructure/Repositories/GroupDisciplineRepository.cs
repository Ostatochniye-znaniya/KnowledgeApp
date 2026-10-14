using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

/// <summary>
/// Выбор дисциплин для групп (ТЗ, п. 2.4).
/// Отдельной таблицы нет: на шаге 2.2 ЛПР выбирает группы, и для каждой создаются
/// тестирования (testing) с заполненными group_id и semester_id. Заведующий кафедрой
/// записывает выбранные дисциплины в testing.discipline_id этих тестирований.
/// </summary>
public class GroupDisciplineRepository
{
    private readonly KnowledgeTestDbContext _context;

    public GroupDisciplineRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    /// <summary>Самый поздний семестр: по году, затем по части (1 — весна, 2 — осень).</summary>
    public async Task<int?> GetLatestSemesterIdAsync()
    {
        return await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.SemesterYear)
            .ThenByDescending(s => s.SemesterPart)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
    }

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
    /// Группы кафедры, у которых есть тестирования в семестре (то есть выбранные для проверки),
    /// с уже выбранными дисциплинами.
    /// </summary>
    public async Task<List<GroupDisciplinesModel>> GetGroupsAsync(int semesterId, int departmentId)
    {
        var rows = await _context.StudyGroups
            .AsNoTracking()
            .Where(g => g.Testings.Any(t => t.SemesterId == semesterId))
            .Where(g => g.StudyProgram!.DepartmentId == departmentId)
            .OrderBy(g => g.GroupNumber)
            .Select(g => new
            {
                g.Id,
                g.GroupNumber,
                g.StudyProgramId,
                StudyProgramName = g.StudyProgram!.Name,
                DepartmentId = g.StudyProgram!.DepartmentId,
                DepartmentName = g.StudyProgram!.Department!.Name,
                FacultyName = g.StudyProgram!.Department!.Faculty!.FacultyName,
                SlotCount = g.Testings.Count(t => t.SemesterId == semesterId),
                Disciplines = g.Testings
                    .Where(t => t.SemesterId == semesterId && t.DisciplineId != null)
                    .OrderBy(t => t.Id)
                    .Select(t => new { t.Discipline!.Id, t.Discipline.Name, t.Discipline.DepartmentId })
                    .ToList()
            })
            .ToListAsync();

        return rows.Select(r => new GroupDisciplinesModel
        {
            GroupId = r.Id,
            GroupNumber = r.GroupNumber,
            StudyProgramId = r.StudyProgramId,
            StudyProgramName = r.StudyProgramName,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.DepartmentName,
            FacultyName = r.FacultyName,
            SemesterId = semesterId,
            SlotCount = r.SlotCount,
            Disciplines = r.Disciplines
                .Select(d => new DisciplineModel(d.Id, d.Name, d.DepartmentId))
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

    /// <summary>Дисциплины кафедры (группа → программа → кафедра → дисциплины).</summary>
    public async Task<List<DisciplineModel>> GetDepartmentDisciplinesAsync(int departmentId)
    {
        var disciplines = await _context.Disciplines
            .AsNoTracking()
            .Where(d => d.DepartmentId == departmentId)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.DepartmentId })
            .ToListAsync();

        return disciplines
            .Select(d => new DisciplineModel(d.Id, d.Name, d.DepartmentId))
            .ToList();
    }

    /// <summary>Тестирования группы в семестре (с отслеживанием изменений), по порядку создания.</summary>
    public Task<List<Testing>> GetGroupTestingsAsync(int groupId, int semesterId)
    {
        return _context.Testings
            .Where(t => t.GroupId == groupId && t.SemesterId == semesterId)
            .OrderBy(t => t.Id)
            .ToListAsync();
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
