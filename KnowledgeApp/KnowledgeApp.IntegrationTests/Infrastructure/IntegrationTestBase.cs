namespace KnowledgeApp.IntegrationTests.Infrastructure;

/// <summary>
/// Базовый класс интеграционного теста: перед каждым тестом очищает БД
/// и наполняет её справочниками из <see cref="TestSeed"/>.
/// </summary>
[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(KnowledgeAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateApiClient();
    }

    protected KnowledgeAppFactory Factory { get; }

    protected HttpClient Client { get; }

    public virtual async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        await using var db = Factory.CreateDbContext();
        await TestSeed.SeedReferenceDataAsync(db);
    }

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
