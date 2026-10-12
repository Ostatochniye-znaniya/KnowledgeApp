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

    /// <summary>
    /// Группы, у которых есть тестирования в семестре (то есть выбранные для проверки),
    /// с уже выбранными дисциплинами. departmentId == null — без фильтра по кафедре.
    /// </summary>
    public async Task<List<GroupDisciplinesModel>> GetGroupsAsync(int semesterId, int? departmentId)
    {
        var rows = await _context.StudyGroups
            .AsNoTracking()
            .Where(g => g.Testings.Any(t => t.SemesterId == semesterId))
            .Where(g => departmentId == null || g.StudyProgram!.DepartmentId == departmentId)
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
    /// Дисциплины, из которых можно выбирать для группы: дисциплины кафедры, за которой
    /// закреплена образовательная программа группы (группа → программа → кафедра → дисциплины).
    /// null — если группы нет.
    /// </summary>
    public async Task<List<DisciplineModel>?> GetAvailableDisciplinesAsync(int groupId)
    {
        var group = await _context.StudyGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { DepartmentId = g.StudyProgram!.DepartmentId })
            .SingleOrDefaultAsync();

        if (group == null)
            return null;

        if (group.DepartmentId == null)
            return new List<DisciplineModel>();

        var disciplines = await _context.Disciplines
            .AsNoTracking()
            .Where(d => d.DepartmentId == group.DepartmentId)
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
