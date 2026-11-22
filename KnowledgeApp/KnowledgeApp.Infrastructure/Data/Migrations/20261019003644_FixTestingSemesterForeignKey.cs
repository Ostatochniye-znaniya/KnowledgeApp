using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTestingSemesterForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "testing_ibfk_4",
                table: "testing");

            migrationBuilder.AddForeignKey(
                name: "testing_ibfk_3",
                table: "testing",
                column: "report_id",
                principalTable: "reports",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "testing_ibfk_4",
                table: "testing",
                column: "semester_id",
                principalTable: "semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "testing_ibfk_3",
                table: "testing");

            migrationBuilder.DropForeignKey(
                name: "testing_ibfk_4",
                table: "testing");

            migrationBuilder.AddForeignKey(
                name: "testing_ibfk_4",
                table: "testing",
                column: "semester_id",
                principalTable: "reports",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
