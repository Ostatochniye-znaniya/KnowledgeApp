using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateLprPlanningAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            // Проверяем дубликаты ДО изменения постоянных таблиц, ничего не удаляем.
            migrationBuilder.Sql("""
                CREATE TEMPORARY TABLE lpr_preflight (ok INT NOT NULL CHECK (ok = 1));
                INSERT INTO lpr_preflight SELECT IF(EXISTS (
                    SELECT 1 FROM recommendation_history GROUP BY study_group_id, semester_id HAVING COUNT(*) > 1
                ), 0, 1);
                INSERT INTO lpr_preflight SELECT IF(EXISTS (
                    SELECT 1 FROM orders WHERE status = 'issued' GROUP BY number HAVING COUNT(*) > 1
                ) OR EXISTS (
                    SELECT 1 FROM orders WHERE status = 'issued' AND (number IS NULL OR TRIM(number) = '')
                ), 0, 1);
                DROP TEMPORARY TABLE lpr_preflight;
                """);
            migrationBuilder.CreateIndex(
                name: "UX_recommendation_history_group_semester",
                table: "recommendation_history",
                columns: new[] { "study_group_id", "semester_id" },
                unique: true);

            migrationBuilder.DropForeignKey(
                name: "FK_recommendation_history_semesters_semester_id",
                table: "recommendation_history");

            migrationBuilder.DropForeignKey(
                name: "FK_recommendation_history_study_groups_study_group_id",
                table: "recommendation_history");

            migrationBuilder.DropForeignKey(
                name: "FK_recommendation_history_users_user_id",
                table: "recommendation_history");


            migrationBuilder.DropIndex(
                name: "IX_recommendation_history_study_group_id",
                table: "recommendation_history");

            migrationBuilder.AddColumn<int>(
                name: "recommendation_history_id",
                table: "testing",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "author",
                table: "recommendation_history",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "manual",
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            // В старой InitialCreate default был фиксированной датой генерации миграции.
            // Меняем только default, не время существующих рекомендаций.
            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at", table: "recommendation_history", type: "datetime(6)",
                nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime), oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "survey_start_date",
                table: "orders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "survey_end_date",
                table: "orders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "results_processing_start_date",
                table: "orders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "preparation_start_date",
                table: "orders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "preparation_end_date",
                table: "orders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "paper_report_deadline",
                table: "orders",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "number",
                table: "orders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.AddColumn<string>(
                name: "creation_token",
                table: "orders",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "online_seminar_date",
                table: "orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_paper_reports",
                table: "orders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "testing_stage_end_date",
                table: "orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "testing_stage_start_date",
                table: "orders",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_number_sequence",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    last_number = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_number_sequence", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.InsertData(
                table: "order_number_sequence",
                columns: new[] { "id", "last_number" },
                values: new object[] { 1, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_testing_recommendation_history_id",
                table: "testing",
                column: "recommendation_history_id");


            migrationBuilder.AddCheckConstraint(
                name: "CK_recommendation_history_author",
                table: "recommendation_history",
                sql: "`author` IN ('automatic', 'manual')");

            migrationBuilder.CreateIndex(
                name: "IX_orders_creation_token",
                table: "orders",
                column: "creation_token",
                unique: true);

            migrationBuilder.Sql("UPDATE orders SET number = NULL WHERE status = 'draft';");
            migrationBuilder.CreateIndex(
                name: "IX_orders_number",
                table: "orders",
                column: "number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_recommendation_history_semesters_semester_id",
                table: "recommendation_history",
                column: "semester_id",
                principalTable: "semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_recommendation_history_study_groups_study_group_id",
                table: "recommendation_history",
                column: "study_group_id",
                principalTable: "study_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_recommendation_history_users_user_id",
                table: "recommendation_history",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_testing_recommendation_history_recommendation_history_id",
                table: "testing",
                column: "recommendation_history_id",
                principalTable: "recommendation_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // При совпадении пары сохраняем user_id уже существующей рекомендации.
            migrationBuilder.Sql("""
                INSERT INTO recommendation_history (study_group_id, semester_id, author, recommended_at, user_id)
                SELECT study_group_id, semester_id, author, recommended_at, NULL FROM planned_testings
                ON DUPLICATE KEY UPDATE author = VALUES(author), recommended_at = VALUES(recommended_at);
                INSERT INTO recommendation_history (study_group_id, semester_id, author, recommended_at, user_id)
                SELECT t.group_id, t.semester_id, 'manual', UTC_TIMESTAMP(6), NULL FROM testing t
                WHERE t.semester_id IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM recommendation_history r WHERE r.study_group_id = t.group_id AND r.semester_id = t.semester_id)
                GROUP BY t.group_id, t.semester_id;
                UPDATE testing t JOIN recommendation_history r
                  ON r.study_group_id = t.group_id AND r.semester_id = t.semester_id
                SET t.recommendation_history_id = r.id WHERE t.semester_id IS NOT NULL;
                UPDATE order_number_sequence SET last_number = COALESCE((
                    SELECT MAX(CAST(SUBSTRING_INDEX(number, '-', 1) AS SIGNED))
                    FROM orders WHERE status = 'issued' AND number REGEXP '^[0-9]+(-ОД)?$'
                ), 0) WHERE id = 1;
                """);

            // Реальные ID только отсутствующим семестрам: восемь прошлых, текущий, два будущих.
            // Стандартный календарь GroupsTestingOptions: Москва, февраль/август.
            migrationBuilder.Sql("""
                SET @lpr_today = DATE(UTC_TIMESTAMP() + INTERVAL 3 HOUR);
                SET @lpr_year = YEAR(@lpr_today) - IF(MONTH(@lpr_today) < 2, 1, 0);
                SET @lpr_part = IF(MONTH(@lpr_today) >= 2 AND MONTH(@lpr_today) < 8, 1, 2);
                SET @lpr_position = @lpr_year * 2 + @lpr_part - 1;
                INSERT INTO semesters (semester_year, semester_part, year_part)
                SELECT FLOOR((@lpr_position + offsets.n) / 2), MOD(@lpr_position + offsets.n, 2) + 1,
                       IF(MOD(@lpr_position + offsets.n, 2) = 0, 2, 1)
                FROM (SELECT -8 n UNION ALL SELECT -7 UNION ALL SELECT -6 UNION ALL SELECT -5
                      UNION ALL SELECT -4 UNION ALL SELECT -3 UNION ALL SELECT -2 UNION ALL SELECT -1
                      UNION ALL SELECT 0 UNION ALL SELECT 1 UNION ALL SELECT 2) offsets
                WHERE NOT EXISTS (SELECT 1 FROM semesters s
                    WHERE s.semester_year = FLOOR((@lpr_position + offsets.n) / 2)
                      AND s.semester_part = MOD(@lpr_position + offsets.n, 2) + 1);
                """);
            // Старую таблицу удаляем ПОСЛЕДНЕЙ, после проверки переноса.
            migrationBuilder.Sql("""
                CREATE TEMPORARY TABLE lpr_transfer_guard (ok INT NOT NULL CHECK (ok = 1));
                INSERT INTO lpr_transfer_guard SELECT IF(EXISTS (
                    SELECT 1 FROM planned_testings p LEFT JOIN recommendation_history r
                      ON r.study_group_id = p.study_group_id AND r.semester_id = p.semester_id
                    WHERE r.id IS NULL OR r.author <> p.author
                ) OR EXISTS (
                    SELECT 1 FROM testing WHERE semester_id IS NOT NULL AND recommendation_history_id IS NULL
                ), 0, 1);
                DROP TEMPORARY TABLE lpr_transfer_guard;
                """);
            migrationBuilder.DropTable(name: "planned_testings");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Перенос назначений и выдачу номеров нельзя откатывать с удалением данных. Восстановите резервную копию.");
    }
}
