using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;

namespace KnowledgeApp.IntegrationTests.Migrations;

/// <summary>
/// Миграции на чистой БД. На старте приложения ошибка миграции только логируется,
/// поэтому сломанная миграция без этих тестов дошла бы до сервера незамеченной.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class MigrationsTests : IAsyncLifetime
{
    private readonly KnowledgeAppFactory _factory;
    private readonly string _databaseName = $"migrations_{Guid.NewGuid():N}";
    private string _rootConnectionString = null!;

    public MigrationsTests(KnowledgeAppFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Отдельная пустая БД в том же контейнере; создать её может только root
        var builder = new MySqlConnectionStringBuilder(_factory.ConnectionString)
        {
            UserID = "root",
            Database = string.Empty
        };
        _rootConnectionString = builder.ConnectionString;
        await ExecuteAsync($"CREATE DATABASE `{_databaseName}`");
    }

    public Task DisposeAsync() => ExecuteAsync($"DROP DATABASE IF EXISTS `{_databaseName}`");

    [Fact]
    public async Task AllMigrations_ApplyToEmptyDatabase()
    {
        await using var db = CreateContext();

        var all = db.Database.GetMigrations().ToList();
        await db.Database.MigrateAsync();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();

        Assert.NotEmpty(all);
        Assert.Equal(all, applied);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    // Down этой миграции намеренно бросает NotSupportedException: заказы без группы
    // нельзя вернуть к обязательной группе. Откатываемость проверяем для всех миграций после неё.
    private const string LastIrreversibleMigration = "20261018171428_MakeOrderGroupOptional";

    [Fact]
    public async Task MigrationsAfterLastIrreversible_RollBackAndApplyAgain()
    {
        await using var db = CreateContext();
        var migrator = db.GetService<IMigrator>();
        var all = db.Database.GetMigrations().ToList();
        Assert.Contains(LastIrreversibleMigration, all);

        await migrator.MigrateAsync();
        await migrator.MigrateAsync(LastIrreversibleMigration);
        Assert.Equal(LastIrreversibleMigration, (await db.Database.GetAppliedMigrationsAsync()).Last());

        await migrator.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public void Model_HasNoChangesWithoutMigration()
    {
        // Аналог `dotnet ef migrations has-pending-model-changes`: снимок последней миграции
        // должен совпадать с текущей моделью, иначе кто-то поменял сущности и не добавил миграцию.
        using var db = CreateContext();
        var snapshotModel = db.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
        Assert.NotNull(snapshotModel);

        if (snapshotModel is IMutableModel mutableModel)
        {
            snapshotModel = mutableModel.FinalizeModel();
        }

        snapshotModel = db.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);

        var differences = db.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(),
            db.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        Assert.True(differences.Count == 0,
            "Модель отличается от снимка миграций: " +
            string.Join("; ", differences.Select(d => d.GetType().Name)));
    }

    private KnowledgeTestDbContext CreateContext()
    {
        var builder = new MySqlConnectionStringBuilder(_rootConnectionString) { Database = _databaseName };
        var options = new DbContextOptionsBuilder<KnowledgeTestDbContext>();
        KnowledgeTestDbContext.ConfigureMySql(options, builder.ConnectionString);
        return new KnowledgeTestDbContext(options.Options);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new MySqlConnection(_rootConnectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
