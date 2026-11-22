using Microsoft.EntityFrameworkCore.Design;

namespace KnowledgeApp.Infrastructure.Context;

// Инструменты EF получают модель без запуска Program.cs (там выполняется Database.Migrate).
// Построение модели не открывает соединение с БД.
public sealed class KnowledgeTestDbContextFactory : IDesignTimeDbContextFactory<KnowledgeTestDbContext>
{
    public KnowledgeTestDbContext CreateDbContext(string[] args) => new();
}
