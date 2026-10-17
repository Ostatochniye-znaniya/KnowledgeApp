using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNullableVars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "report_id",
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
                defaultValue: new DateTime(2026, 9, 8, 18, 1, 40, 189, DateTimeKind.Utc).AddTicks(628),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 8, 12, 13, 5, 434, DateTimeKind.Utc).AddTicks(7639));

            migrationBuilder.AlterColumn<DateOnly>(
                name: "job_end",
                table: "employee_rigths_requests",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "report_id",
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
                defaultValue: new DateTime(2026, 9, 8, 12, 13, 5, 434, DateTimeKind.Utc).AddTicks(7639),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 8, 18, 1, 40, 189, DateTimeKind.Utc).AddTicks(628));

            migrationBuilder.AlterColumn<DateOnly>(
                name: "job_end",
                table: "employee_rigths_requests",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
