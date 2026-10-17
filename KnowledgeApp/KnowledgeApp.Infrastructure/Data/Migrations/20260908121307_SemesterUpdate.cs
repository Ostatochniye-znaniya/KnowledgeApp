using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SemesterUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "year_part",
                table: "semesters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2026, 9, 8, 12, 13, 5, 434, DateTimeKind.Utc).AddTicks(7639),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 3, 2, 17, 6, 19, 47, DateTimeKind.Utc).AddTicks(9191));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "year_part",
                table: "semesters");

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2026, 3, 2, 17, 6, 19, 47, DateTimeKind.Utc).AddTicks(9191),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 8, 12, 13, 5, 434, DateTimeKind.Utc).AddTicks(7639));
        }
    }
}
