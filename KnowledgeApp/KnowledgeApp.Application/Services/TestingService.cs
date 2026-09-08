
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public class TestingService
{
    private readonly TestingRepository _testingRepository;
    private readonly KnowledgeTestDbContext _context;

    public TestingService(TestingRepository testingRepository, KnowledgeTestDbContext context)
    {
        _testingRepository = testingRepository;
        _context = context;
    }

    public async Task<IEnumerable<Testing>> GetAll()
    {
        return await _testingRepository.GetAll();
    }

    public async Task<TestingModel> GetById(int id)
    {
        return await _testingRepository.GetById(id);
    }

    public async Task<TestingModel> Create(TestingModel testingModel)
    {
        return await _testingRepository.Create(testingModel);
    }

    public async Task<TestingModel> Update(TestingModel testingModel)
    {
        return await _testingRepository.Update(testingModel);
    }

    public async Task<bool> Delete(int id)
    {
        return await _testingRepository.Delete(id);
    }

    public async Task<List<TestingModel>> GetByGroupId(int groupId)
    {
        return await _testingRepository.GetByGroupId(groupId);
    }

    public async Task<List<TestingModel>> GetByDepartmentId(int departmentId)
    {
        return await _testingRepository.GetByDepartmentId(departmentId);
    }

    public async Task<List<TestingScheduleDto>> GetTestingScheduleAsync()
    {
        var query = from t in _context.Testings
                    join sg in _context.StudyGroups on t.GroupId equals sg.Id
                    join sp in _context.StudyPrograms on sg.StudyProgramId equals sp.Id
                    join d in _context.Disciplines on t.DisciplineId equals d.Id
                    join dep in _context.Departments on d.DepartmentId equals dep.Id
                    join dt in _context.DisciplineTeachers on d.Id equals dt.DisciplineId into dtJoin
                    from dt in dtJoin.DefaultIfEmpty()
                    join u in _context.Users on dt.ResponsibleTeacherId equals u.Id into uJoin
                    from u in uJoin.DefaultIfEmpty()
                    where t.Status == "Scheduled"
                    orderby t.ScheduledDate, t.ScheduledTime
                    select new TestingScheduleDto
                    {
                        Id = t.Id,  // ДОБАВЛЕНО
                        GroupName = sg.GroupNumber,
                        ProgramName = sp.Name,
                        DisciplineName = d.Name,
                        DepartmentName = dep.Name,
                        TeacherName = u != null ? u.Name : "Не назначен",
                        Date = t.ScheduledDate ?? DateTime.MinValue,
                        Time = t.ScheduledTime ?? TimeSpan.Zero
                    };

        var result = await query.ToListAsync();

        for (int i = 0; i < result.Count; i++)
        {
            result[i].Number = i + 1;
        }

        return result;
    }

    public async Task<List<TestingScheduleWithFacultyDto>> GetTestingScheduleWithFacultyAsync(int? semesterId = null)
    {
        var query = from t in _context.Testings
                    join sg in _context.StudyGroups on t.GroupId equals sg.Id
                    join sp in _context.StudyPrograms on sg.StudyProgramId equals sp.Id
                    join dep in _context.Departments on sp.DepartmentId equals dep.Id
                    join f in _context.Faculties on dep.FacultyId equals f.Id
                    join d in _context.Disciplines on t.DisciplineId equals d.Id
                    join dt in _context.DisciplineTeachers on d.Id equals dt.DisciplineId into dtJoin
                    from dt in dtJoin.DefaultIfEmpty()
                    join u in _context.Users on dt.ResponsibleTeacherId equals u.Id into uJoin
                    from u in uJoin.DefaultIfEmpty()
                    select new TestingScheduleWithFacultyDto
                    {
                        Id = t.Id,  // ДОБАВЛЕНО
                        FacultyName = f.FacultyName,
                        FacultyId = f.Id,
                        GroupName = sg.GroupNumber,
                        ProgramName = sp.Name,
                        DisciplineName = d.Name,
                        DepartmentName = dep.Name,
                        TeacherName = u != null ? u.Name : "Не назначен",
                        Date = t.ScheduledDate ?? DateTime.MinValue,
                        Time = t.ScheduledTime ?? TimeSpan.Zero,
                        SemesterId = t.SemesterId ?? 0
                    };

        if (semesterId.HasValue && semesterId.Value > 0)
        {
            var semester = await _context.Semesters.FindAsync(semesterId.Value);
            if (semester != null)
            {
                DateTime startDate, endDate;

                if (semester.SemesterPart == 1)
                {
                    startDate = new DateTime(semester.SemesterYear, 2, 1);
                    endDate = new DateTime(semester.SemesterYear, 6, 30);
                }
                else
                {
                    startDate = new DateTime(semester.SemesterYear, 9, 1);
                    endDate = new DateTime(semester.SemesterYear, 12, 31);
                }

                query = query.Where(x => x.Date >= startDate && x.Date <= endDate);
            }
        }

        var result = await query
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.Date)
            .ThenBy(x => x.Time)
            .ToListAsync();

        for (int i = 0; i < result.Count; i++)
        {
            result[i].Number = i + 1;
        }

        return result;
    }

    public async Task<List<FacultyDto>> GetFacultiesAsync()
    {
        var faculties = await _context.Faculties
            .Select(f => new FacultyDto
            {
                Id = f.Id,
                Name = f.FacultyName
            })
            .ToListAsync();

        return faculties;
    }

    public async Task<List<SemesterDto>> GetSemestersAsync()
    {
        var semesters = await _context.Semesters
            .Select(s => new SemesterDto
            {
                Id = s.Id,
                Year = s.SemesterYear,
                Part = s.SemesterPart,
                Name = $"{s.SemesterYear} год, {(s.SemesterPart == 1 ? "весенний" : "осенний")} семестр"
            })
            .ToListAsync();

        return semesters;
    }

    public async Task<SemesterDto> GetCurrentSemesterAsync()
    {
        var now = DateTime.Now;
        int currentYear = now.Year;
        int currentPart = (now.Month >= 2 && now.Month <= 7) ? 1 : 2;

        var currentSemester = await _context.Semesters
            .Where(s => s.SemesterYear == currentYear && s.SemesterPart == currentPart)
            .Select(s => new SemesterDto
            {
                Id = s.Id,
                Year = s.SemesterYear,
                Part = s.SemesterPart,
                Name = $"{s.SemesterYear} год, {(s.SemesterPart == 1 ? "весенний" : "осенний")} семестр"
            })
            .FirstOrDefaultAsync();

        if (currentSemester == null)
        {
            currentSemester = await _context.Semesters
                .OrderBy(s => s.SemesterYear)
                .ThenBy(s => s.SemesterPart)
                .Select(s => new SemesterDto
                {
                    Id = s.Id,
                    Year = s.SemesterYear,
                    Part = s.SemesterPart,
                    Name = $"{s.SemesterYear} год, {(s.SemesterPart == 1 ? "весенний" : "осенний")} семестр"
                })
                .FirstOrDefaultAsync();
        }

        return currentSemester;
    }
}