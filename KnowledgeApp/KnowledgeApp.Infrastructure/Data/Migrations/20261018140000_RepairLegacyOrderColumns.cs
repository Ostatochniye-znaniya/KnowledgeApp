using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeApp.Infrastructure.Data.Migrations;

// AddOrders был изменён в develop после применения в старых БД.
// Этот переход выполняется ДО MakeOrderGroupOptional и не подставляет выдуманные даты.
[DbContext(typeof(KnowledgeTestDbContext))]
[Migration("20261018140000_RepairLegacyOrderColumns")]
public sealed class RepairLegacyOrderColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddIfMissing(migrationBuilder, "group_id", "int NULL");
        foreach (var column in new[] { "survey_start_date", "survey_end_date", "preparation_start_date",
                     "preparation_end_date", "results_processing_start_date" })
            AddIfMissing(migrationBuilder, column, "date NULL");
        Conditional(migrationBuilder,
            "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'orders' AND INDEX_NAME = 'IX_orders_group_id'",
            "CREATE INDEX IX_orders_group_id ON orders (group_id)");
        Conditional(migrationBuilder,
            "SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'orders' AND CONSTRAINT_NAME = 'orders_group_fk'",
            "ALTER TABLE orders ADD CONSTRAINT orders_group_fk FOREIGN KEY (group_id) REFERENCES study_groups (id) ON DELETE RESTRICT");
    }

    private static void AddIfMissing(MigrationBuilder builder, string column, string definition) =>
        Conditional(builder,
            $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'orders' AND COLUMN_NAME = '{column}'",
            $"ALTER TABLE orders ADD COLUMN `{column}` {definition}");

    private static void Conditional(MigrationBuilder builder, string existsQuery, string ddl) => builder.Sql($"""
        SET @lpr_repair_sql = IF(({existsQuery}) = 0, '{ddl}', 'SELECT 1');
        PREPARE lpr_repair_stmt FROM @lpr_repair_sql;
        EXECUTE lpr_repair_stmt;
        DEALLOCATE PREPARE lpr_repair_stmt;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Восстановленные столбцы приказов нельзя удалять автоматически: они могут содержать данные.");
}
