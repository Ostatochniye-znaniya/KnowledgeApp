using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    public partial class AddTeacherTestingApproval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "testing_teacher_approval",
                columns: table => new
                {
                    testing_id = table.Column<int>(type: "int", nullable: false),
                    teacher_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    responded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.testing_id, x.teacher_id });
                    table.ForeignKey(
                        name: "FK_testing_teacher_approval_testing_testing_id",
                        column: x => x.testing_id,
                        principalTable: "testing",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_testing_teacher_approval_users_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "teacher_id",
                table: "testing_teacher_approval",
                column: "teacher_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "testing_teacher_approval");
        }
    }
}
