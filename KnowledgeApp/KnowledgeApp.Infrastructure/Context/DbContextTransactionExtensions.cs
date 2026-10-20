using System.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Context;

/// <summary>
/// Транзакции при включённом EnableRetryOnFailure: EF запрещает BeginTransaction напрямую
/// («does not support user-initiated transactions»), транзакция должна выполняться
/// внутри стратегии повторов, чтобы при сбое повторилась целиком.
/// </summary>
public static class DbContextTransactionExtensions
{
    public static Task InTransactionAsync(
        this DbContext context,
        Func<Task> operation,
        CancellationToken cancellationToken = default) =>
        context.InTransactionAsync(null, operation, cancellationToken);

    public static Task InTransactionAsync(
        this DbContext context,
        IsolationLevel? isolationLevel,
        Func<Task> operation,
        CancellationToken cancellationToken = default) =>
        context.InTransactionAsync<bool>(isolationLevel, async () =>
        {
            await operation();
            return true;
        }, cancellationToken);

    public static Task<T> InTransactionAsync<T>(
        this DbContext context,
        IsolationLevel? isolationLevel,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = isolationLevel.HasValue
                ? await context.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
                : await context.Database.BeginTransactionAsync(cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
