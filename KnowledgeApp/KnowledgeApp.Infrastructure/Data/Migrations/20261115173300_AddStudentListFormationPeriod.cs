using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentListFormationPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "student_list_formation_end_date",
                table: "semesters",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "student_list_formation_start_date",
                table: "semesters",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_semesters_student_list_period",
                table: "semesters",
                sql: "(`student_list_formation_start_date` IS NULL AND `student_list_formation_end_date` IS NULL) OR (`student_list_formation_start_date` IS NOT NULL AND `student_list_formation_end_date` IS NOT NULL AND `student_list_formation_end_date` >= `student_list_formation_start_date`)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_semesters_student_list_period",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "student_list_formation_end_date",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "student_list_formation_start_date",
                table: "semesters");
        }
    }
}
