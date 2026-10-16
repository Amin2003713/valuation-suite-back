using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ToolContentI18nAndPerToolSubmissions : Migration
    {
        /// <summary>
        ///     tool code → per-tool submissions table created by this migration.
        ///     Used by the Up (copy shared-table rows in) and Down (copy them back)
        ///     data steps so the split never deletes data.
        /// </summary>
        private static readonly (string Code, string Table)[] ToolTables =
        [
            ("IDEA-ASSESS", "IdeaAssessmentSubmissions"),
            ("INNOV-READ", "InnovationReadinessSubmissions"),
            ("IP-AUDIT", "IpAuditSubmissions"),
            ("JOB-EVAL", "JobEvaluationSubmissions"),
            ("WIPO-DIAG", "WipoDiagnosticsSubmissions"),
            ("IPSCORE", "IpscoreSubmissions"),
            ("PATENT-SEARCH", "PatentSearchSubmissions"),
            ("STARTUP-VAL", "StartupValuationSubmissions"),
            ("BRAND-VAL", "BrandValuationSubmissions"),
            ("TRADEMARK-VAL", "TrademarkValuationSubmissions"),
            ("PATENT-VAL", "PatentValuationSubmissions"),
            ("PHARMA-IP", "PharmaPatentValuationSubmissions"),
            ("INTANGIBLE", "IntangibleAssetsSubmissions"),
            ("KNOWHOW", "KnowhowValuationSubmissions"),
            ("ICON-SCORECARD", "IconScorecardSubmissions"),
            ("BINOMIAL", "BinomialValuationSubmissions"),
            ("SOFT-VAL", "SoftwareValuationSubmissions"),
            ("RNPV", "RnpvCalculatorSubmissions"),
            ("PVI", "PatentValueIndexSubmissions"),
            ("KNOWHOW-PRO", "KnowhowProSubmissions"),
            ("EXPORT-READ", "ExportReadinessSubmissions"),
            ("DIGITAL-AI", "DigitalAiReadinessSubmissions"),
            ("MATURITY", "MaturityAssessmentSubmissions"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionNotes_ToolSubmissions_SubmissionId",
                table: "SubmissionNotes");

            migrationBuilder.AddColumn<string>(
                name: "AnswerType",
                table: "ToolQuestions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HelpText",
                table: "ToolQuestions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "ToolQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Placeholder",
                table: "ToolQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScaleMax",
                table: "ToolQuestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScaleMin",
                table: "ToolQuestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslationsJson",
                table: "ToolQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TranslationsJson",
                table: "ToolForms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BinomialValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinomialValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinomialValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BrandValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DigitalAiReadinessSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalAiReadinessSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalAiReadinessSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExportReadinessSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportReadinessSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportReadinessSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IconScorecardSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IconScorecardSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IconScorecardSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IdeaAssessmentSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaAssessmentSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaAssessmentSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InnovationReadinessSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnovationReadinessSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InnovationReadinessSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IntangibleAssetsSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntangibleAssetsSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntangibleAssetsSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IpAuditSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAuditSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpAuditSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IpscoreSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpscoreSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpscoreSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobEvaluationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEvaluationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobEvaluationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KnowhowProSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowhowProSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowhowProSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KnowhowValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "MaturityAssessmentSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaturityAssessmentSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaturityAssessmentSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatentSearchSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatentSearchSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatentSearchSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatentValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatentValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatentValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatentValueIndexSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatentValueIndexSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatentValueIndexSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PharmaPatentValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmaPatentValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmaPatentValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RnpvCalculatorSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RnpvCalculatorSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RnpvCalculatorSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SoftwareValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StartupValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StartupValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrademarkValuationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrademarkValuationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrademarkValuationSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WipoDiagnosticsSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WipoDiagnosticsSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WipoDiagnosticsSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BinomialValuationSubmissions_CreatedAt",
                table: "BinomialValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BinomialValuationSubmissions_Industry",
                table: "BinomialValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_BinomialValuationSubmissions_UserId",
                table: "BinomialValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BinomialValuationSubmissions_UserId_ToolCode",
                table: "BinomialValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_BrandValuationSubmissions_CreatedAt",
                table: "BrandValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BrandValuationSubmissions_Industry",
                table: "BrandValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_BrandValuationSubmissions_UserId",
                table: "BrandValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BrandValuationSubmissions_UserId_ToolCode",
                table: "BrandValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalAiReadinessSubmissions_CreatedAt",
                table: "DigitalAiReadinessSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalAiReadinessSubmissions_Industry",
                table: "DigitalAiReadinessSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalAiReadinessSubmissions_UserId",
                table: "DigitalAiReadinessSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalAiReadinessSubmissions_UserId_ToolCode",
                table: "DigitalAiReadinessSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportReadinessSubmissions_CreatedAt",
                table: "ExportReadinessSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReadinessSubmissions_Industry",
                table: "ExportReadinessSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReadinessSubmissions_UserId",
                table: "ExportReadinessSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReadinessSubmissions_UserId_ToolCode",
                table: "ExportReadinessSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_IconScorecardSubmissions_CreatedAt",
                table: "IconScorecardSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IconScorecardSubmissions_Industry",
                table: "IconScorecardSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_IconScorecardSubmissions_UserId",
                table: "IconScorecardSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IconScorecardSubmissions_UserId_ToolCode",
                table: "IconScorecardSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAssessmentSubmissions_CreatedAt",
                table: "IdeaAssessmentSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAssessmentSubmissions_Industry",
                table: "IdeaAssessmentSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAssessmentSubmissions_UserId",
                table: "IdeaAssessmentSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAssessmentSubmissions_UserId_ToolCode",
                table: "IdeaAssessmentSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_InnovationReadinessSubmissions_CreatedAt",
                table: "InnovationReadinessSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationReadinessSubmissions_Industry",
                table: "InnovationReadinessSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationReadinessSubmissions_UserId",
                table: "InnovationReadinessSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationReadinessSubmissions_UserId_ToolCode",
                table: "InnovationReadinessSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_IntangibleAssetsSubmissions_CreatedAt",
                table: "IntangibleAssetsSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntangibleAssetsSubmissions_Industry",
                table: "IntangibleAssetsSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_IntangibleAssetsSubmissions_UserId",
                table: "IntangibleAssetsSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntangibleAssetsSubmissions_UserId_ToolCode",
                table: "IntangibleAssetsSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditSubmissions_CreatedAt",
                table: "IpAuditSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditSubmissions_Industry",
                table: "IpAuditSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditSubmissions_UserId",
                table: "IpAuditSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditSubmissions_UserId_ToolCode",
                table: "IpAuditSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_IpscoreSubmissions_CreatedAt",
                table: "IpscoreSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IpscoreSubmissions_Industry",
                table: "IpscoreSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_IpscoreSubmissions_UserId",
                table: "IpscoreSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpscoreSubmissions_UserId_ToolCode",
                table: "IpscoreSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationSubmissions_CreatedAt",
                table: "JobEvaluationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationSubmissions_Industry",
                table: "JobEvaluationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationSubmissions_UserId",
                table: "JobEvaluationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationSubmissions_UserId_ToolCode",
                table: "JobEvaluationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowProSubmissions_CreatedAt",
                table: "KnowhowProSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowProSubmissions_Industry",
                table: "KnowhowProSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowProSubmissions_UserId",
                table: "KnowhowProSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowProSubmissions_UserId_ToolCode",
                table: "KnowhowProSubmissions",
                columns: new[] { "UserId", "ToolCode" });

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

            migrationBuilder.CreateIndex(
                name: "IX_MaturityAssessmentSubmissions_CreatedAt",
                table: "MaturityAssessmentSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaturityAssessmentSubmissions_Industry",
                table: "MaturityAssessmentSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_MaturityAssessmentSubmissions_UserId",
                table: "MaturityAssessmentSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaturityAssessmentSubmissions_UserId_ToolCode",
                table: "MaturityAssessmentSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PatentSearchSubmissions_CreatedAt",
                table: "PatentSearchSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatentSearchSubmissions_Industry",
                table: "PatentSearchSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_PatentSearchSubmissions_UserId",
                table: "PatentSearchSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatentSearchSubmissions_UserId_ToolCode",
                table: "PatentSearchSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PatentValuationSubmissions_CreatedAt",
                table: "PatentValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatentValuationSubmissions_Industry",
                table: "PatentValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_PatentValuationSubmissions_UserId",
                table: "PatentValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatentValuationSubmissions_UserId_ToolCode",
                table: "PatentValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PatentValueIndexSubmissions_CreatedAt",
                table: "PatentValueIndexSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatentValueIndexSubmissions_Industry",
                table: "PatentValueIndexSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_PatentValueIndexSubmissions_UserId",
                table: "PatentValueIndexSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatentValueIndexSubmissions_UserId_ToolCode",
                table: "PatentValueIndexSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmaPatentValuationSubmissions_CreatedAt",
                table: "PharmaPatentValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PharmaPatentValuationSubmissions_Industry",
                table: "PharmaPatentValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_PharmaPatentValuationSubmissions_UserId",
                table: "PharmaPatentValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmaPatentValuationSubmissions_UserId_ToolCode",
                table: "PharmaPatentValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_RnpvCalculatorSubmissions_CreatedAt",
                table: "RnpvCalculatorSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RnpvCalculatorSubmissions_Industry",
                table: "RnpvCalculatorSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_RnpvCalculatorSubmissions_UserId",
                table: "RnpvCalculatorSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RnpvCalculatorSubmissions_UserId_ToolCode",
                table: "RnpvCalculatorSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareValuationSubmissions_CreatedAt",
                table: "SoftwareValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareValuationSubmissions_Industry",
                table: "SoftwareValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareValuationSubmissions_UserId",
                table: "SoftwareValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareValuationSubmissions_UserId_ToolCode",
                table: "SoftwareValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_StartupValuationSubmissions_CreatedAt",
                table: "StartupValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StartupValuationSubmissions_Industry",
                table: "StartupValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_StartupValuationSubmissions_UserId",
                table: "StartupValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StartupValuationSubmissions_UserId_ToolCode",
                table: "StartupValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkValuationSubmissions_CreatedAt",
                table: "TrademarkValuationSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkValuationSubmissions_Industry",
                table: "TrademarkValuationSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkValuationSubmissions_UserId",
                table: "TrademarkValuationSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkValuationSubmissions_UserId_ToolCode",
                table: "TrademarkValuationSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            migrationBuilder.CreateIndex(
                name: "IX_WipoDiagnosticsSubmissions_CreatedAt",
                table: "WipoDiagnosticsSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WipoDiagnosticsSubmissions_Industry",
                table: "WipoDiagnosticsSubmissions",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_WipoDiagnosticsSubmissions_UserId",
                table: "WipoDiagnosticsSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WipoDiagnosticsSubmissions_UserId_ToolCode",
                table: "WipoDiagnosticsSubmissions",
                columns: new[] { "UserId", "ToolCode" });

            /* ── Data migration: copy every existing shared-table row into its
             *    per-tool table. Language is backfilled from the stored input
             *    (the client has always sent `lang`), Status = 1 (Completed). ── */
            foreach (var (code, table) in ToolTables)
                migrationBuilder.Sql($@"
INSERT INTO [{table}] ([Id],[IsActive],[CreatedBy],[CreatedAt],[ModifiedBy],[ModifiedAt],[DeletedBy],[DeletedAt],[UserId],[ToolCode],[Name],[InputJson],[ResultJson],[OverallScore],[Language],[Status],[CompanyId],[CompanyName],[Industry])
SELECT [Id],[IsActive],[CreatedBy],[CreatedAt],[ModifiedBy],[ModifiedAt],[DeletedBy],[DeletedAt],[UserId],[ToolCode],[Name],[InputJson],[ResultJson],[OverallScore],
       CASE WHEN [InputJson] LIKE '%""lang"":""en""%' THEN N'en' ELSE N'fa' END, 1, [CompanyId],[CompanyName],[Industry]
FROM [ToolSubmissions] WHERE [ToolCode] = N'{code}';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /* ── Reversible: copy every per-tool row back into the shared table
             *    (ids not already present) BEFORE the per-tool tables are dropped,
             *    so Down loses no data and the restored FK stays valid. ── */
            foreach (var (code, table) in ToolTables)
                migrationBuilder.Sql($@"
INSERT INTO [ToolSubmissions] ([Id],[IsActive],[CreatedBy],[CreatedAt],[ModifiedBy],[ModifiedAt],[DeletedBy],[DeletedAt],[UserId],[ToolCode],[Name],[InputJson],[ResultJson],[OverallScore],[Language],[Status],[CompanyId],[CompanyName],[Industry])
SELECT [s].[Id],[s].[IsActive],[s].[CreatedBy],[s].[CreatedAt],[s].[ModifiedBy],[s].[ModifiedAt],[s].[DeletedBy],[s].[DeletedAt],[s].[UserId],[s].[ToolCode],[s].[Name],[s].[InputJson],[s].[ResultJson],[s].[OverallScore],[s].[Language],[s].[Status],[s].[CompanyId],[s].[CompanyName],[s].[Industry]
FROM [{table}] [s]
WHERE NOT EXISTS (SELECT 1 FROM [ToolSubmissions] [t] WHERE [t].[Id] = [s].[Id]);");

            migrationBuilder.DropTable(
                name: "BinomialValuationSubmissions");

            migrationBuilder.DropTable(
                name: "BrandValuationSubmissions");

            migrationBuilder.DropTable(
                name: "DigitalAiReadinessSubmissions");

            migrationBuilder.DropTable(
                name: "ExportReadinessSubmissions");

            migrationBuilder.DropTable(
                name: "IconScorecardSubmissions");

            migrationBuilder.DropTable(
                name: "IdeaAssessmentSubmissions");

            migrationBuilder.DropTable(
                name: "InnovationReadinessSubmissions");

            migrationBuilder.DropTable(
                name: "IntangibleAssetsSubmissions");

            migrationBuilder.DropTable(
                name: "IpAuditSubmissions");

            migrationBuilder.DropTable(
                name: "IpscoreSubmissions");

            migrationBuilder.DropTable(
                name: "JobEvaluationSubmissions");

            migrationBuilder.DropTable(
                name: "KnowhowProSubmissions");

            migrationBuilder.DropTable(
                name: "KnowhowValuationSubmissions");

            migrationBuilder.DropTable(
                name: "MaturityAssessmentSubmissions");

            migrationBuilder.DropTable(
                name: "PatentSearchSubmissions");

            migrationBuilder.DropTable(
                name: "PatentValuationSubmissions");

            migrationBuilder.DropTable(
                name: "PatentValueIndexSubmissions");

            migrationBuilder.DropTable(
                name: "PharmaPatentValuationSubmissions");

            migrationBuilder.DropTable(
                name: "RnpvCalculatorSubmissions");

            migrationBuilder.DropTable(
                name: "SoftwareValuationSubmissions");

            migrationBuilder.DropTable(
                name: "StartupValuationSubmissions");

            migrationBuilder.DropTable(
                name: "TrademarkValuationSubmissions");

            migrationBuilder.DropTable(
                name: "WipoDiagnosticsSubmissions");

            migrationBuilder.DropColumn(
                name: "AnswerType",
                table: "ToolQuestions");

            migrationBuilder.DropColumn(
                name: "HelpText",
                table: "ToolQuestions");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "ToolQuestions");

            migrationBuilder.DropColumn(
                name: "Placeholder",
                table: "ToolQuestions");

            migrationBuilder.DropColumn(
                name: "ScaleMax",
                table: "ToolQuestions");

            migrationBuilder.DropColumn(
                name: "ScaleMin",
                table: "ToolQuestions");

            migrationBuilder.DropColumn(
                name: "TranslationsJson",
                table: "ToolQuestions");

            migrationBuilder.DropColumn(
                name: "TranslationsJson",
                table: "ToolForms");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionNotes_ToolSubmissions_SubmissionId",
                table: "SubmissionNotes",
                column: "SubmissionId",
                principalTable: "ToolSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
