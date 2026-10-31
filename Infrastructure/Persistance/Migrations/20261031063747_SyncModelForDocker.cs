using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelForDocker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowhowValuationLocalizations");

            migrationBuilder.DropTable(
                name: "KnowhowValuationQuestions");

            migrationBuilder.DropTable(
                name: "KnowhowValuationSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowhowValuationLocalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HelpText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SectionTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowhowValuationLocalizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowhowValuationQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HelpText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Placeholder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScaleMax = table.Column<int>(type: "int", nullable: true),
                    ScaleMin = table.Column<int>(type: "int", nullable: true),
                    SectionKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SectionTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowhowValuationQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowhowValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowhowValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowhowValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowValuationLocalizations_Language_QuestionId",
                table: "KnowhowValuationLocalizations",
                columns: new[] { "Language", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowValuationQuestions_QuestionId",
                table: "KnowhowValuationQuestions",
                column: "QuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowValuationQuestions_SortOrder",
                table: "KnowhowValuationQuestions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowValuationSubmissions_CreatedAt",
                table: "KnowhowValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowValuationSubmissions_Industry",
                table: "KnowhowValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowValuationSubmissions_UserId",
                table: "KnowhowValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowValuationSubmissions_UserId_ToolCode",
                table: "KnowhowValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });
        }
    }
}
