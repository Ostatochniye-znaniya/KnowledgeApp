using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingRecommendedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "recommended_date",
                table: "testing",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recommended_pair",
                table: "testing",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "recommended_date",
                table: "testing");

            migrationBuilder.DropColumn(
                name: "recommended_pair",
                table: "testing");
        }
    }
}
