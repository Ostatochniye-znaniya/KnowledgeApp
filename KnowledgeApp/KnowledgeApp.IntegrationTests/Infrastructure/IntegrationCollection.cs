namespace KnowledgeApp.IntegrationTests.Infrastructure;

/// <summary>
/// Все интеграционные тесты делят один контейнер MySQL и одно приложение
/// и выполняются последовательно: каждый класс сам сбрасывает и наполняет БД.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<KnowledgeAppFactory>
{
    public const string Name = "Integration";
}
