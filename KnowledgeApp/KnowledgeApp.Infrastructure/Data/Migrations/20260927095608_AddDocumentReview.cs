using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "file_name",
                table: "reports",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "review_comment",
                table: "reports",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "reports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by_user_id",
                table: "reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "reports",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "uploaded_at",
                table: "reports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2026, 9, 27, 9, 56, 7, 608, DateTimeKind.Utc).AddTicks(6189),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 24, 23, 17, 45, 438, DateTimeKind.Utc).AddTicks(8436));

            migrationBuilder.CreateTable(
                name: "schedule_documents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    faculty_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    file_key = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    uploaded_by_user_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    review_comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reviewed_by_user_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "schedule_documents_faculty_fk",
                        column: x => x.faculty_id,
                        principalTable: "faculties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "schedule_documents_reviewed_by_fk",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "schedule_documents_semester_fk",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "schedule_documents_uploaded_by_fk",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateIndex(
                name: "IX_reports_reviewed_by_user_id",
                table: "reports",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "reports_status",
                table: "reports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_documents_reviewed_by_user_id",
                table: "schedule_documents",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_documents_semester_id",
                table: "schedule_documents",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_documents_uploaded_by_user_id",
                table: "schedule_documents",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "schedule_documents_faculty_semester",
                table: "schedule_documents",
                columns: new[] { "faculty_id", "semester_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "schedule_documents_status",
                table: "schedule_documents",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "reports_reviewed_by_fk",
                table: "reports",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "reports_reviewed_by_fk",
                table: "reports");

            migrationBuilder.DropTable(
                name: "schedule_documents");

            migrationBuilder.DropIndex(
                name: "IX_reports_reviewed_by_user_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "reports_status",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "file_name",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "review_comment",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "status",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "uploaded_at",
                table: "reports");

            migrationBuilder.AlterColumn<DateTime>(
                name: "recommended_at",
                table: "recommendation_history",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2026, 9, 24, 23, 17, 45, 438, DateTimeKind.Utc).AddTicks(8436),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValue: new DateTime(2026, 9, 27, 9, 56, 7, 608, DateTimeKind.Utc).AddTicks(6189));
        }
    }
}
