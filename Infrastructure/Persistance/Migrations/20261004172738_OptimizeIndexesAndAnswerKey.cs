using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeIndexesAndAnswerKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Answers_AttemptId",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_ToolSubmissions_CreatedAt",
                table: "ToolSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ToolSubmissions_ToolCode",
                table: "ToolSubmissions",
                column: "ToolCode");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_CreatedAt",
                table: "Payments",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubmissionId",
                table: "Payments",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CreatedAt",
                table: "AspNetUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_AttemptId",
                table: "Answers",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_AttemptId_QuestionId",
                table: "Answers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ToolSubmissions_CreatedAt",
                table: "ToolSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ToolSubmissions_ToolCode",
                table: "ToolSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_CreatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SubmissionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_AttemptId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_AttemptId_QuestionId",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_AttemptId",
                table: "Answers",
                column: "AttemptId",
                unique: true);
        }
    }
}
