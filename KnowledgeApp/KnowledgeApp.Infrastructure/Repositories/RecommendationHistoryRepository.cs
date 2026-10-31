using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Planning;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using KnowledgeApp.Infrastructure.Entities;

namespace KnowledgeApp.Infrastructure.Repositories;

public class RecommendationHistoryRepository
{
    private readonly KnowledgeTestDbContext _context;

    public RecommendationHistoryRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public Task AddRecommendationAsync(RecommendationHistoryModel model) => Mutate(async () =>
    {
        ValidateAuthor(model.Author);
        var entity = new RecommendationHistory
        {
            RecommendedAt = model.RecommendedAt, Author = model.Author,
            UserId = model.RecommendedById, SemesterId = model.SemesterId, StudyGroupId = model.StudyGroupId
        };
        _context.RecommendationHistory.Add(entity);
        await _context.SaveChangesAsync();
        model.Id = entity.Id;
    });

    public Task DeleteRecommendationAsync(int recommendationId) => Mutate(async () =>
    {
        var entity = await _context.RecommendationHistory.FindAsync(recommendationId)
            ?? throw new KeyNotFoundException("Рекомендация не найдена");
        if (await _context.Testings.AnyAsync(t => t.RecommendationHistoryId == recommendationId))
            throw new PlanningRuleException("Нельзя удалить назначение, связанное с тестированиями");
        _context.RecommendationHistory.Remove(entity);
        await _context.SaveChangesAsync();
    });

    public async Task<RecommendationHistoryModel> GetById(int recommendationId)
    {
        var res = await _context.RecommendationHistory.Include(u => u.StudyGroup).Include(u => u.Semester).Include(r=>r.User).FirstOrDefaultAsync(r => r.Id == recommendationId);
        if (res == null) throw new KeyNotFoundException("Рекомендация не найдена");
        return new RecommendationHistoryModel()
        {
            Id = recommendationId,
            RecommendedAt = res.RecommendedAt,
            RecommendedBy = res.User?.Name, RecommendedById = res.UserId, Author = res.Author,
            SemesterId = res.SemesterId,
            StudyGroupId = res.StudyGroupId,
            StudyGroupName = res.StudyGroup?.GroupNumber
        };
    }
    public Task UpdateRecommendationAsync(RecommendationHistoryModel model) => Mutate(async () =>
    {
        ValidateAuthor(model.Author);
        var existing = await _context.RecommendationHistory.FindAsync(model.Id)
            ?? throw new KeyNotFoundException("Рекомендация не найдена");
        if ((existing.StudyGroupId != model.StudyGroupId || existing.SemesterId != model.SemesterId)
            && await _context.Testings.AnyAsync(t => t.RecommendationHistoryId == model.Id))
            throw new PlanningRuleException("Нельзя менять группу или семестр назначения, связанного с тестированиями");
        existing.RecommendedAt = model.RecommendedAt;
        existing.Author = model.Author;
        // Старые клиенты передавали имя вместо ID. Без нового ID ответственного сохраняем.
        if (model.RecommendedById.HasValue) existing.UserId = model.RecommendedById;
        existing.SemesterId = model.SemesterId;
        existing.StudyGroupId = model.StudyGroupId;
        await _context.SaveChangesAsync();
    });

    private Task Mutate(Func<Task> action) =>
        _context.InTransactionAsync(System.Data.IsolationLevel.RepeatableRead, async () =>
        {
            _context.ChangeTracker.Clear();
            var state = await _context.LockTestingPlan();
            state.Version++;
            await action();
        });

    private static void ValidateAuthor(string author)
    {
        if (author is not ("manual" or "automatic"))
            throw new ArgumentException("Источник назначения должен быть manual или automatic");
    }

    public async Task<List<RecommendationHistoryModel>> GetAllAsync(
        int? semesterId = null,
        int? studyGroupId = null,
        int? userId = null)
    {
        var query = _context.RecommendationHistory
            .Include(u => u.StudyGroup)
            .Include(u => u.Semester)
            .Include(u=> u.User)
            .AsQueryable();

        if (semesterId.HasValue)
        {
            query = query.Where(r => r.SemesterId == semesterId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        if (studyGroupId.HasValue)
        {
            query = query.Where(r => r.StudyGroupId == studyGroupId.Value);
        }

        var results = await query
            .OrderByDescending(r => r.RecommendedAt)
            .ToListAsync();

        return results.Select(res => new RecommendationHistoryModel()
        {
            Id = res.Id,
            RecommendedAt = res.RecommendedAt,
            RecommendedBy = res.User?.Name, RecommendedById = res.UserId, Author = res.Author,
            SemesterId = res.SemesterId,
            StudyGroupId = res.StudyGroupId,
            StudyGroupName = res.StudyGroup?.GroupNumber
        }).ToList();
    }
}
