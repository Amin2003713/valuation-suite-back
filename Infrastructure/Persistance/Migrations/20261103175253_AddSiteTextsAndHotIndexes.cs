using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteTextsAndHotIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteTexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GroupKey = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TextFa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TextEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTexts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WipoDiagnosticsSubmissions_Industry_CreatedAt",
                table: "WipoDiagnosticsSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WipoDiagnosticsSubmissions_UserId_CreatedAt",
                table: "WipoDiagnosticsSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkValuationSubmissions_Industry_CreatedAt",
                table: "TrademarkValuationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkValuationSubmissions_UserId_CreatedAt",
                table: "TrademarkValuationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StartupValuationSubmissions_Industry_CreatedAt",
                table: "StartupValuationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StartupValuationSubmissions_UserId_CreatedAt",
                table: "StartupValuationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StartupAssessmentSubmissions_Industry_CreatedAt",
                table: "StartupAssessmentSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StartupAssessmentSubmissions_UserId_CreatedAt",
                table: "StartupAssessmentSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareValuationSubmissions_Industry_CreatedAt",
                table: "SoftwareValuationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareValuationSubmissions_UserId_CreatedAt",
                table: "SoftwareValuationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RnpvCalculatorSubmissions_Industry_CreatedAt",
                table: "RnpvCalculatorSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RnpvCalculatorSubmissions_UserId_CreatedAt",
                table: "RnpvCalculatorSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmaPatentValuationSubmissions_Industry_CreatedAt",
                table: "PharmaPatentValuationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmaPatentValuationSubmissions_UserId_CreatedAt",
                table: "PharmaPatentValuationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatentValueIndexSubmissions_Industry_CreatedAt",
                table: "PatentValueIndexSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatentValueIndexSubmissions_UserId_CreatedAt",
                table: "PatentValueIndexSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatentValuationSubmissions_Industry_CreatedAt",
                table: "PatentValuationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatentValuationSubmissions_UserId_CreatedAt",
                table: "PatentValuationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MaturityAssessmentSubmissions_Industry_CreatedAt",
                table: "MaturityAssessmentSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MaturityAssessmentSubmissions_UserId_CreatedAt",
                table: "MaturityAssessmentSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowProSubmissions_Industry_CreatedAt",
                table: "KnowhowProSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowhowProSubmissions_UserId_CreatedAt",
                table: "KnowhowProSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationSubmissions_Industry_CreatedAt",
                table: "JobEvaluationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationSubmissions_UserId_CreatedAt",
                table: "JobEvaluationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IpscoreSubmissions_Industry_CreatedAt",
                table: "IpscoreSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IpscoreSubmissions_UserId_CreatedAt",
                table: "IpscoreSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditSubmissions_Industry_CreatedAt",
                table: "IpAuditSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditSubmissions_UserId_CreatedAt",
                table: "IpAuditSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntangibleAssetsSubmissions_Industry_CreatedAt",
                table: "IntangibleAssetsSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntangibleAssetsSubmissions_UserId_CreatedAt",
                table: "IntangibleAssetsSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InnovationReadinessSubmissions_Industry_CreatedAt",
                table: "InnovationReadinessSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InnovationReadinessSubmissions_UserId_CreatedAt",
                table: "InnovationReadinessSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAssessmentSubmissions_Industry_CreatedAt",
                table: "IdeaAssessmentSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAssessmentSubmissions_UserId_CreatedAt",
                table: "IdeaAssessmentSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IconScorecardSubmissions_Industry_CreatedAt",
                table: "IconScorecardSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IconScorecardSubmissions_UserId_CreatedAt",
                table: "IconScorecardSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportReadinessSubmissions_Industry_CreatedAt",
                table: "ExportReadinessSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportReadinessSubmissions_UserId_CreatedAt",
                table: "ExportReadinessSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalAiReadinessSubmissions_Industry_CreatedAt",
                table: "DigitalAiReadinessSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalAiReadinessSubmissions_UserId_CreatedAt",
                table: "DigitalAiReadinessSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BrandValuationSubmissions_Industry_CreatedAt",
                table: "BrandValuationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BrandValuationSubmissions_UserId_CreatedAt",
                table: "BrandValuationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BinomialValuationSubmissions_Industry_CreatedAt",
                table: "BinomialValuationSubmissions",
                columns: new[] { "Industry", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BinomialValuationSubmissions_UserId_CreatedAt",
                table: "BinomialValuationSubmissions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTexts_GroupKey_SortOrder",
                table: "SiteTexts",
                columns: new[] { "GroupKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTexts_TextKey",
                table: "SiteTexts",
                column: "TextKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteTexts");

            migrationBuilder.DropIndex(
                name: "IX_WipoDiagnosticsSubmissions_Industry_CreatedAt",
                table: "WipoDiagnosticsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_WipoDiagnosticsSubmissions_UserId_CreatedAt",
                table: "WipoDiagnosticsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_TrademarkValuationSubmissions_Industry_CreatedAt",
                table: "TrademarkValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_TrademarkValuationSubmissions_UserId_CreatedAt",
                table: "TrademarkValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_StartupValuationSubmissions_Industry_CreatedAt",
                table: "StartupValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_StartupValuationSubmissions_UserId_CreatedAt",
                table: "StartupValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_StartupAssessmentSubmissions_Industry_CreatedAt",
                table: "StartupAssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_StartupAssessmentSubmissions_UserId_CreatedAt",
                table: "StartupAssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareValuationSubmissions_Industry_CreatedAt",
                table: "SoftwareValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareValuationSubmissions_UserId_CreatedAt",
                table: "SoftwareValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_RnpvCalculatorSubmissions_Industry_CreatedAt",
                table: "RnpvCalculatorSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_RnpvCalculatorSubmissions_UserId_CreatedAt",
                table: "RnpvCalculatorSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PharmaPatentValuationSubmissions_Industry_CreatedAt",
                table: "PharmaPatentValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PharmaPatentValuationSubmissions_UserId_CreatedAt",
                table: "PharmaPatentValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PatentValueIndexSubmissions_Industry_CreatedAt",
                table: "PatentValueIndexSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PatentValueIndexSubmissions_UserId_CreatedAt",
                table: "PatentValueIndexSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PatentValuationSubmissions_Industry_CreatedAt",
                table: "PatentValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PatentValuationSubmissions_UserId_CreatedAt",
                table: "PatentValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_MaturityAssessmentSubmissions_Industry_CreatedAt",
                table: "MaturityAssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_MaturityAssessmentSubmissions_UserId_CreatedAt",
                table: "MaturityAssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_KnowhowProSubmissions_Industry_CreatedAt",
                table: "KnowhowProSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_KnowhowProSubmissions_UserId_CreatedAt",
                table: "KnowhowProSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_JobEvaluationSubmissions_Industry_CreatedAt",
                table: "JobEvaluationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_JobEvaluationSubmissions_UserId_CreatedAt",
                table: "JobEvaluationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpscoreSubmissions_Industry_CreatedAt",
                table: "IpscoreSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpscoreSubmissions_UserId_CreatedAt",
                table: "IpscoreSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpAuditSubmissions_Industry_CreatedAt",
                table: "IpAuditSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpAuditSubmissions_UserId_CreatedAt",
                table: "IpAuditSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IntangibleAssetsSubmissions_Industry_CreatedAt",
                table: "IntangibleAssetsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IntangibleAssetsSubmissions_UserId_CreatedAt",
                table: "IntangibleAssetsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_InnovationReadinessSubmissions_Industry_CreatedAt",
                table: "InnovationReadinessSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_InnovationReadinessSubmissions_UserId_CreatedAt",
                table: "InnovationReadinessSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IdeaAssessmentSubmissions_Industry_CreatedAt",
                table: "IdeaAssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IdeaAssessmentSubmissions_UserId_CreatedAt",
                table: "IdeaAssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IconScorecardSubmissions_Industry_CreatedAt",
                table: "IconScorecardSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IconScorecardSubmissions_UserId_CreatedAt",
                table: "IconScorecardSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ExportReadinessSubmissions_Industry_CreatedAt",
                table: "ExportReadinessSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ExportReadinessSubmissions_UserId_CreatedAt",
                table: "ExportReadinessSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_DigitalAiReadinessSubmissions_Industry_CreatedAt",
                table: "DigitalAiReadinessSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_DigitalAiReadinessSubmissions_UserId_CreatedAt",
                table: "DigitalAiReadinessSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_BrandValuationSubmissions_Industry_CreatedAt",
                table: "BrandValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_BrandValuationSubmissions_UserId_CreatedAt",
                table: "BrandValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_BinomialValuationSubmissions_Industry_CreatedAt",
                table: "BinomialValuationSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_BinomialValuationSubmissions_UserId_CreatedAt",
                table: "BinomialValuationSubmissions");
        }
    }
}
