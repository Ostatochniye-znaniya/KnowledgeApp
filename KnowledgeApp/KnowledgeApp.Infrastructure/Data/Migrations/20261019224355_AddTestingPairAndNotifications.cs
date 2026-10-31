using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingPairAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "scheduled_pair",
                table: "testing",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workflow_notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    recipient_user_id = table.Column<int>(type: "int", nullable: true),
                    recipient_department_id = table.Column<int>(type: "int", nullable: true),
                    testing_id = table.Column<int>(type: "int", nullable: true),
                    type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_notifications_departments_recipient_department_id",
                        column: x => x.recipient_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_notifications_testing_testing_id",
                        column: x => x.testing_id,
                        principalTable: "testing",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_notifications_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_notifications_department_read_created",
                table: "workflow_notifications",
                columns: new[] { "recipient_department_id", "read_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_notifications_testing_id",
                table: "workflow_notifications",
                column: "testing_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_notifications_user_read_created",
                table: "workflow_notifications",
                columns: new[] { "recipient_user_id", "read_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "recipient_department_id",
                table: "workflow_notifications",
                column: "recipient_department_id");

            migrationBuilder.CreateIndex(
                name: "recipient_user_id",
                table: "workflow_notifications",
                column: "recipient_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_notifications");

            migrationBuilder.DropColumn(
                name: "scheduled_pair",
                table: "testing");
        }
    }
}
