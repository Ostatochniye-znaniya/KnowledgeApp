using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingSelectionPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "teacher_id",
                table: "testing_teacher_approval",
                newName: "teacher_id1");

            migrationBuilder.AddColumn<DateTime>(
                name: "selection_period_end",
                table: "testing",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "selection_period_start",
                table: "testing",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selection_period_end",
                table: "testing");

            migrationBuilder.DropColumn(
                name: "selection_period_start",
                table: "testing");

            migrationBuilder.RenameIndex(
                name: "teacher_id1",
                table: "testing_teacher_approval",
                newName: "teacher_id");
        }
    }
}
