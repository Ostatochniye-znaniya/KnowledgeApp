using System.Diagnostics;
using System.Threading.Channels;
using KnowledgeApp.Infrastructure.Entities;

namespace KnowledgeApp.API.Logging;

public sealed class DatabaseLoggerProvider : ILoggerProvider
{
    private static readonly AsyncLocal<bool> Suppressed = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Channel<SystemLogEntry> _channel = Channel.CreateBounded<SystemLogEntry>(
        new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public DatabaseLoggerProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ChannelReader<SystemLogEntry> Reader => _channel.Reader;

    public ILogger CreateLogger(string categoryName) => new DatabaseLogger(categoryName, this);

    public void Dispose() => _channel.Writer.TryComplete();

    public static IDisposable Suppress()
    {
        Suppressed.Value = true;
        return new SuppressionScope();
    }

    public static string? CurrentTraceId(HttpContext? httpContext) =>
        Activity.Current?.TraceId.ToHexString() ?? httpContext?.TraceIdentifier;

    private void Write(string category, LogLevel level, string message, Exception? exception)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        _channel.Writer.TryWrite(new SystemLogEntry
        {
            CreatedAt = DateTime.UtcNow,
            Level = LevelCode(level),
            Service = ServiceName(category),
            Category = Cut(category, 255)!,
            Message = Cut(string.IsNullOrWhiteSpace(message) ? exception?.Message ?? category : message, 2000)!,
            TraceId = Cut(CurrentTraceId(httpContext), 64),
            Details = Cut(exception?.ToString(), 20000),
            RequestMethod = Cut(httpContext?.Request.Method, 16),
            RequestPath = Cut(httpContext is null ? null : $"{httpContext.Request.PathBase}{httpContext.Request.Path}", 512)
        });
    }

    private static bool ShouldStore(string category, LogLevel level) =>
        !Suppressed.Value &&
        level != LogLevel.None &&
        (level >= LogLevel.Warning ||
         (level == LogLevel.Information && category.StartsWith("KnowledgeApp", StringComparison.Ordinal)));

    private static string LevelCode(LogLevel level) => level switch
    {
        >= LogLevel.Error => "error",
        LogLevel.Warning => "warning",
        _ => "info"
    };

    private static string ServiceName(string category)
    {
        if (category.StartsWith("KnowledgeApp", StringComparison.Ordinal))
            return Cut(category[(category.LastIndexOf('.') + 1)..], 128)!;
        if (category.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            return "EF Core";
        if (category.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            return "ASP.NET Core";
        if (category.StartsWith("System.Net.Http", StringComparison.Ordinal))
            return "HttpClient";
        return Cut(category, 128)!;
    }

    private static string? Cut(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];

    private sealed class SuppressionScope : IDisposable
    {
        public void Dispose() => Suppressed.Value = false;
    }

    private sealed class DatabaseLogger : ILogger
    {
        private readonly string _category;
        private readonly DatabaseLoggerProvider _provider;

        public DatabaseLogger(string category, DatabaseLoggerProvider provider)
        {
            _category = category;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => ShouldStore(_category, logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            _provider.Write(_category, logLevel, formatter(state, exception), exception);
        }
    }
}
