using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.API.Logging;

public sealed class SystemLogWriter : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);
    private readonly DatabaseLoggerProvider _provider;
    private readonly IServiceScopeFactory _scopeFactory;
    private DateTime _nextCleanup = DateTime.MinValue;

    public SystemLogWriter(DatabaseLoggerProvider provider, IServiceScopeFactory scopeFactory)
    {
        _provider = provider;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var suppression = DatabaseLoggerProvider.Suppress();
        var batch = new List<SystemLogEntry>(BatchSize);
        try
        {
            while (await _provider.Reader.WaitToReadAsync(stoppingToken))
            {
                while (batch.Count < BatchSize && _provider.Reader.TryRead(out var entry))
                    batch.Add(entry);
                await Flush(batch, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            while (batch.Count < BatchSize && _provider.Reader.TryRead(out var entry))
                batch.Add(entry);
            await Flush(batch, CancellationToken.None);
        }
    }

    private async Task Flush(List<SystemLogEntry> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<KnowledgeTestDbContext>();
            context.SystemLogs.AddRange(batch);
            await context.SaveChangesAsync(cancellationToken);

            if (DateTime.UtcNow >= _nextCleanup)
            {
                var threshold = DateTime.UtcNow - RetentionPeriod;
                await context.SystemLogs.Where(l => l.CreatedAt < threshold).ExecuteDeleteAsync(cancellationToken);
                _nextCleanup = DateTime.UtcNow + CleanupInterval;
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Console.Error.WriteLine($"Не удалось записать технические логи в БД: {e.Message}");
        }
        finally
        {
            batch.Clear();
        }
    }
}
