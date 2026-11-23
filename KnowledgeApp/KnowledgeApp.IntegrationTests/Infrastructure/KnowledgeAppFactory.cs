using KnowledgeApp.Application.DTOs.MockAuth;
using KnowledgeApp.Application.Services.MockAuth;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.MySql;

namespace KnowledgeApp.IntegrationTests.Infrastructure;

/// <summary>
/// Поднимает MySQL в Testcontainers, применяет миграции и запускает API в памяти.
/// Один экземпляр на всю коллекцию <see cref="IntegrationCollection"/>.
/// </summary>
public class KnowledgeAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string PathBase = "/csh/api";

    // Та же версия, что в docker-compose и в MySqlServerVersion контекста
    private readonly MySqlContainer _mySql = new MySqlBuilder("mysql:9.1").Build();

    public string ConnectionString => _mySql.GetConnectionString();

    /// <summary>Настройки S3 для API; по умолчанию заглушка, тесты хранилища подставляют MinIO.</summary>
    public S3StorageOptions S3Options { get; set; } = new()
    {
        Endpoint = "http://localhost:9",
        AccessKey = "test",
        SecretKey = "test-secret",
        Bucket = "knowledgeapp-test"
    };

    public async Task InitializeAsync()
    {
        await _mySql.StartAsync();

        // Program.cs проглатывает ошибки миграций на старте, поэтому применяем их явно
        // до запуска приложения и отключаем стартовый Migrate.
        Environment.SetEnvironmentVariable("SKIP_DATABASE_MIGRATIONS", "true");

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _mySql.DisposeAsync();
    }

    public KnowledgeTestDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<KnowledgeTestDbContext>();
        KnowledgeTestDbContext.ConfigureMySql(builder, ConnectionString);
        return new KnowledgeTestDbContext(builder.Options);
    }

    /// <summary>Клиент с базовым адресом, включающим PathBase, как за nginx: "Role/GetRoles".</summary>
    public HttpClient CreateApiClient() => CreateApiClient(this);

    /// <summary>Клиент к приложению с выключенной MOCK-авторизацией.</summary>
    public HttpClient CreateApiClientWithoutMockAuth(Action<IServiceCollection>? configure = null) =>
        CreateApiClient(WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                ReplaceMockAuth(services, enabled: false);
                configure?.Invoke(services);
            })));

    /// <summary>Удаляет данные из всех таблиц, кроме истории миграций.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var db = CreateDbContext();
        var tables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' " +
                "AND TABLE_NAME <> '__EFMigrationsHistory'")
            .ToListAsync();

        var sql = "SET FOREIGN_KEY_CHECKS = 0; " +
                  string.Concat(tables.Select(t => $"TRUNCATE TABLE `{t}`; ")) +
                  "SET FOREIGN_KEY_CHECKS = 1;";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<KnowledgeTestDbContext>>();
            services.AddDbContext<KnowledgeTestDbContext>(options =>
                KnowledgeTestDbContext.ConfigureMySql(options, ConnectionString));

            services.RemoveAll<S3StorageOptions>();
            services.AddSingleton(_ => S3Options);

            ReplaceMockAuth(services, enabled: true);
        });
    }

    private static void ReplaceMockAuth(IServiceCollection services, bool enabled)
    {
        var options = new MockAuthOptions
        {
            Enabled = enabled,
            DefaultRole = "Admin",
            HeaderName = "X-Mock-Role"
        };

        services.RemoveAll<IOptions<MockAuthOptions>>();
        services.RemoveAll<IMockAuthService>();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<IMockAuthService>(new MockAuthService(options));
    }

    private static HttpClient CreateApiClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://localhost{PathBase}/"),
            HandleCookies = false
        });
}
