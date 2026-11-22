using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHodGroupParticipation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "group_participation_decision_id",
                table: "testing",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "study_groups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "group_participation_decisions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    study_program_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    recommended_group_id = table.Column<int>(type: "int", nullable: true),
                    selected_group_id = table.Column<int>(type: "int", nullable: true),
                    reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    decided_by_user_id = table.Column<int>(type: "int", nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_participation_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_participation_decisions_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_participation_decisions_study_groups_recommended_group~",
                        column: x => x.recommended_group_id,
                        principalTable: "study_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_participation_decisions_study_groups_selected_group_id",
                        column: x => x.selected_group_id,
                        principalTable: "study_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_participation_decisions_study_programs_study_program_id",
                        column: x => x.study_program_id,
                        principalTable: "study_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_participation_decisions_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateIndex(
                name: "IX_testing_group_participation_decision_id",
                table: "testing",
                column: "group_participation_decision_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_participation_decisions_decided_by_user_id",
                table: "group_participation_decisions",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_participation_decisions_recommended_group_id",
                table: "group_participation_decisions",
                column: "recommended_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_participation_decisions_selected_group_id",
                table: "group_participation_decisions",
                column: "selected_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_participation_decisions_semester_id",
                table: "group_participation_decisions",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_participation_decisions_study_program_id_semester_id",
                table: "group_participation_decisions",
                columns: new[] { "study_program_id", "semester_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_testing_group_participation_decisions_group_participation_de~",
                table: "testing",
                column: "group_participation_decision_id",
                principalTable: "group_participation_decisions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_testing_group_participation_decisions_group_participation_de~",
                table: "testing");

            migrationBuilder.DropTable(
                name: "group_participation_decisions");

            migrationBuilder.DropIndex(
                name: "IX_testing_group_participation_decision_id",
                table: "testing");

            migrationBuilder.DropColumn(
                name: "group_participation_decision_id",
                table: "testing");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "study_groups");
        }
    }
}
