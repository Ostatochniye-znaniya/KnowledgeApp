using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Context;

public static class TestingPlanLock
{
    // Все записи плана и привязка реальных тестирований используют один порядок блокировок.
    public static async Task<TestingPlanningState> LockTestingPlan(this KnowledgeTestDbContext context,
        CancellationToken ct = default) =>
        (await context.TestingPlanningStates
            .FromSqlRaw("SELECT * FROM testing_planning_state WHERE id = 1 FOR UPDATE").ToListAsync(ct)).Single();
}
