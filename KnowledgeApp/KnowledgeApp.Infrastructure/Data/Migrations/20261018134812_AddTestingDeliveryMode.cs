using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingDeliveryMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_in_person",
                table: "testing",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 27, 9, 56, 7, 608, DateTimeKind.Utc).AddTicks(6189));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_in_person",
                table: "testing");

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2026, 9, 27, 9, 56, 7, 608, DateTimeKind.Utc).AddTicks(6189),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");
        }
    }
}
