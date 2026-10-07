using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNullableTestingVars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "discipline_id",
                table: "testing",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2026, 9, 24, 23, 17, 45, 438, DateTimeKind.Utc).AddTicks(8436),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 8, 18, 1, 40, 189, DateTimeKind.Utc).AddTicks(628));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "discipline_id",
                table: "testing",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2026, 9, 8, 18, 1, 40, 189, DateTimeKind.Utc).AddTicks(628),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 24, 23, 17, 45, 438, DateTimeKind.Utc).AddTicks(8436));
        }
    }
}
