using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsTestingPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planned_testings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    study_group_id = table.Column<int>(type: "int", nullable: false),
                    author = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recommended_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.CheckConstraint("CK_planned_testings_author", "`author` IN ('automatic', 'manual')");
                    table.ForeignKey(
                        name: "FK_planned_testings_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_planned_testings_study_groups_study_group_id",
                        column: x => x.study_group_id,
                        principalTable: "study_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "testing_planning_state",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    current_semester_id = table.Column<int>(type: "int", nullable: true),
                    initialized_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.CheckConstraint("CK_testing_planning_state_singleton", "`id` = 1");
                    table.ForeignKey(
                        name: "FK_testing_planning_state_semesters_current_semester_id",
                        column: x => x.current_semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.InsertData(
                table: "testing_planning_state",
                columns: new[] { "id", "current_semester_id", "initialized_at", "version" },
                values: new object[] { 1, null, null, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_planned_testings_semester_id",
                table: "planned_testings",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "UX_planned_testings_group_semester",
                table: "planned_testings",
                columns: new[] { "study_group_id", "semester_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_testing_planning_state_current_semester_id",
                table: "testing_planning_state",
                column: "current_semester_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planned_testings");

            migrationBuilder.DropTable(
                name: "testing_planning_state");

        }
    }
}
