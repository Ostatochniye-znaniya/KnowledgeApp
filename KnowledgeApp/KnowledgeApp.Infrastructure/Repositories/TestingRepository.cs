using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

public class TestingRepository
{
    private readonly KnowledgeTestDbContext _context;

    public TestingRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Testing>> GetAll()
    {
        return await _context.Set<Testing>().ToListAsync();
    }

    public async Task<TestingModel> GetById(int id)
    {
        var entity = await _context.Testings.FindAsync(id);
        if (entity == null) throw new Exception($"Тестирование с ID {id} не найдено");
        return ToModel(entity);
    }

    public async Task<TestingModel> Create(TestingModel model)
    {
        ValidateModel(model);

        var entity = new Testing
        {
            GroupId = model.GroupId,
            DisciplineId = model.DisciplineId,
            ScheduledDate = model.ScheduledDate,
            ScheduledTime = model.ScheduledTime,
            Room = model.Room,
            LmsUrl = model.LmsUrl,
            IsInPerson = model.IsInPerson,
            Status = model.Status,
            ResultOfTesting = model.ResultOfTesting,
            ReportId = model.ReportId,
            SemesterId = model.SemesterId,
            RecommendationHistory = await ResolveRecommendation(model)
        };

        await _context.Testings.AddAsync(entity);
        await _context.SaveChangesAsync();

        return ToModel(entity);
    }

    public async Task<TestingModel> Update(TestingModel model)
    {
        var entity = await _context.Testings.FindAsync(model.Id);
        if (entity == null) throw new Exception($"Тестирование с ID {model.Id} не найдено");

        ValidateModel(model);

        var scheduleChanged = entity.ScheduledDate != model.ScheduledDate ||
                              entity.ScheduledTime != model.ScheduledTime;
        if (scheduleChanged)
        {
            var approvals = await _context.TestingTeacherApprovals
                .Where(a => a.TestingId == model.Id)
                .ToListAsync();
            foreach (var approval in approvals)
            {
                approval.Status = "pending";
                approval.Comment = null;
                approval.RespondedAt = null;
                _context.WorkflowNotifications.Add(new WorkflowNotification
                {
                    RecipientUserId = approval.TeacherId,
                    TestingId = model.Id,
                    Type = "testing_time_updated",
                    Title = "Изменилось время проверки",
                    Message = model.ScheduledDate.HasValue && model.ScheduledTime.HasValue
                        ? $"Предложено время: {model.ScheduledDate.Value:dd.MM.yyyy} в {model.ScheduledTime.Value:hh\\:mm}. Подтвердите/отклоните или выберите удобную пару в заданном периоде."
                        : "Время проверки изменено. Выберите удобную пару в заданном периоде.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            entity.ScheduledPair = null;
        }

        entity.GroupId = model.GroupId;
        entity.DisciplineId = model.DisciplineId;
        entity.ScheduledDate = model.ScheduledDate;
        entity.ScheduledTime = model.ScheduledTime;
        entity.Room = model.Room;
        entity.LmsUrl = model.LmsUrl;
        entity.IsInPerson = model.IsInPerson;
        entity.Status = model.Status;
        entity.ResultOfTesting = model.ResultOfTesting;
        entity.ReportId = model.ReportId;
        entity.SemesterId = model.SemesterId;
        entity.RecommendationHistory = await ResolveRecommendation(model);
        if (entity.RecommendationHistory is null) entity.RecommendationHistoryId = null;

        await _context.SaveChangesAsync();

        return ToModel(entity);
    }

    public async Task<TestingModel?> UpdateDelivery(
        int id,
        bool isInPerson,
        string? room,
        string? lmsUrl,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Testings.FindAsync([id], cancellationToken);
        if (entity == null)
            return null;

        entity.IsInPerson = isInPerson;
        entity.Room = room;
        entity.LmsUrl = lmsUrl;
        await _context.SaveChangesAsync(cancellationToken);
        return ToModel(entity);
    }

    public async Task<bool> Delete(int id)
    {
        var entity = await _context.Testings.FindAsync(id);
        if (entity == null) throw new Exception($"Тестирование с ID {id} не найдено");

        _context.Testings.Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<TestingModel>> GetByGroupId(int groupId)
    {
        var entities = await _context.Testings
            .Where(t => t.GroupId == groupId)
            .AsNoTracking()
            .ToListAsync();

        return entities.Select(e => ToModel(e)).ToList();
    }

    public async Task<List<TestingModel>> GetByDepartmentId(int departmentId)
    {
        List<int> studyProgramIds = await _context.StudyPrograms
            .Where(sp => sp.DepartmentId == departmentId)
            .Select(sp => sp.Id)
            .ToListAsync();
        List<int> studyGroupIds = await _context.StudyGroups
            .Where(sg => studyProgramIds.Contains(sg.StudyProgramId))
            .Select(sg => sg.Id)
            .ToListAsync();
        List<int> disciplineIds = await _context.Disciplines
            .Where(d => d.DepartmentId == departmentId)
            .Select(d => d.Id)
            .ToListAsync();
        List<int> reportIds = await _context.Reports
            .Where(r => r.DisciplineId.HasValue && disciplineIds.Contains(r.DisciplineId.GetValueOrDefault()))
            .Select(r => r.Id)
            .ToListAsync();

        if (!disciplineIds.Any())
        {
            return new List<TestingModel>();
        }

        var testings = await _context.Testings
            .Where(t => studyGroupIds.Contains(t.GroupId) || reportIds.Contains(t.ReportId.Value))
            .ToListAsync();

        return testings.Select(e => ToModel(e)).ToList();
    }

    // 👉 МЕТОД GetTestingScheduleAsync УДАЛЕН - он теперь в TestingService

    private async Task<RecommendationHistory?> ResolveRecommendation(TestingModel model)
    {
        if (!model.SemesterId.HasValue) return null;
        var recommendation = await _context.RecommendationHistory.SingleOrDefaultAsync(r =>
            r.StudyGroupId == model.GroupId && r.SemesterId == model.SemesterId.Value);
        if (recommendation is not null) return recommendation;
        return new RecommendationHistory
        {
            StudyGroupId = model.GroupId, SemesterId = model.SemesterId.Value,
            // Создатель Testing может быть преподавателем, поэтому не выдаём его ID за ответственного ЛПР.
            Author = "manual", UserId = null, RecommendedAt = DateTime.UtcNow
        };
    }

    private void ValidateModel(TestingModel model)
    {
        if (!_context.StudyGroups.Any(g => g.Id == model.GroupId))
            throw new Exception($"Учебная группа с ID {model.GroupId} не существует");

        if (!_context.Disciplines.Any(d => d.Id == model.DisciplineId))
            throw new Exception($"Дисциплина с ID {model.DisciplineId} не существует");

        if (!_context.Semesters.Any(s => s.Id == model.SemesterId))
            throw new Exception($"Семестр с ID {model.SemesterId} не существует");

        if (!_context.Reports.Any(r => r.Id == model.ReportId))
            throw new Exception($"Отчет с ID {model.ReportId} не существует");
    }

    private TestingModel ToModel(Testing entity)
    {
        return new TestingModel(
            entity.Id,
            entity.GroupId,
            entity.DisciplineId,
            entity.ScheduledDate,
            entity.ScheduledTime,
            entity.Status,
            entity.ResultOfTesting,
            entity.ReportId,
            entity.SemesterId,
            entity.Room,
            entity.LmsUrl,
            entity.IsInPerson ?? !string.IsNullOrWhiteSpace(entity.Room)
        );
    }
}
