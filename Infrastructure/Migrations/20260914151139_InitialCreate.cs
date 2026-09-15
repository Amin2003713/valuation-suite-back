using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalculationConfigs",
                columns: table => new
                {
                    OutputKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    InputQuestionIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResultLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationConfigs", x => x.OutputKey);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Plan = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MathExpressions",
                columns: table => new
                {
                    Expression = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PostfixNotation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MathExpressions", x => x.Expression);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CurrentDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assessments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Plan = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyId1",
                        column: x => x.CompanyId1,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MathOperations",
                columns: table => new
                {
                    Type = table.Column<int>(type: "int", nullable: false),
                    Operand = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Constant = table.Column<double>(type: "float", nullable: true),
                    MathExpressionId = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MathOperations", x => x.Type);
                    table.ForeignKey(
                        name: "FK_MathOperations_MathExpressions_MathExpressionId",
                        column: x => x.MathExpressionId,
                        principalTable: "MathExpressions",
                        principalColumn: "Expression",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MathVariables",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinValue = table.Column<double>(type: "float", nullable: false),
                    MaxValue = table.Column<double>(type: "float", nullable: false),
                    DefaultValue = table.Column<double>(type: "float", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MathExpressionId = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MathVariables", x => x.Name);
                    table.ForeignKey(
                        name: "FK_MathVariables_MathExpressions_MathExpressionId",
                        column: x => x.MathExpressionId,
                        principalTable: "MathExpressions",
                        principalColumn: "Expression",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssessmentId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentVersions_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentVersions_Assessments_AssessmentId1",
                        column: x => x.AssessmentId1,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TotalSteps = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedSteps = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientRevision = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VersionId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_AssessmentVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "AssessmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_AssessmentVersions_VersionId1",
                        column: x => x.VersionId1,
                        principalTable: "AssessmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    VersionId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Steps_AssessmentVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "AssessmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Steps_AssessmentVersions_VersionId1",
                        column: x => x.VersionId1,
                        principalTable: "AssessmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NumericValue = table.Column<double>(type: "float", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChoiceValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ClientRevision = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SynchronizedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Answers_AssessmentAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "AssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Answers_AssessmentAttempts_AttemptId1",
                        column: x => x.AttemptId1,
                        principalTable: "AssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LevelLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsCalculated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_AssessmentAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "AssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_AssessmentAttempts_AttemptId1",
                        column: x => x.AttemptId1,
                        principalTable: "AssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HelpText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CalculationOutputKey = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    MathExpressionExpression = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    StepId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_CalculationConfigs_CalculationOutputKey",
                        column: x => x.CalculationOutputKey,
                        principalTable: "CalculationConfigs",
                        principalColumn: "OutputKey");
                    table.ForeignKey(
                        name: "FK_Questions_MathExpressions_MathExpressionExpression",
                        column: x => x.MathExpressionExpression,
                        principalTable: "MathExpressions",
                        principalColumn: "Expression");
                    table.ForeignKey(
                        name: "FK_Questions_Steps_StepId",
                        column: x => x.StepId,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Questions_Steps_StepId1",
                        column: x => x.StepId1,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoredAnswer",
                columns: table => new
                {
                    ResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: false),
                    CalculatedValue = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoredAnswer", x => new { x.ResultId, x.Id });
                    table.ForeignKey(
                        name: "FK_ScoredAnswer_AssessmentResults_ResultId",
                        column: x => x.ResultId,
                        principalTable: "AssessmentResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Score = table.Column<double>(type: "float", nullable: true, defaultValue: 0.0),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Options_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationRules",
                columns: table => new
                {
                    Field = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MinValue = table.Column<double>(type: "float", nullable: true),
                    MaxValue = table.Column<double>(type: "float", nullable: true),
                    MinLength = table.Column<int>(type: "int", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    Pattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRules", x => x.Field);
                    table.ForeignKey(
                        name: "FK_ValidationRules_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisibilityConditions",
                columns: table => new
                {
                    TargetQuestionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisibilityConditions", x => x.TargetQuestionId);
                    table.ForeignKey(
                        name: "FK_VisibilityConditions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_AttemptId",
                table: "Answers",
                column: "AttemptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_AttemptId1",
                table: "Answers",
                column: "AttemptId1");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_SynchronizedAt",
                table: "Answers",
                column: "SynchronizedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_Status",
                table: "AssessmentAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_UserId",
                table: "AssessmentAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_UserId_VersionId",
                table: "AssessmentAttempts",
                columns: new[] { "UserId", "VersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_VersionId",
                table: "AssessmentAttempts",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_VersionId1",
                table: "AssessmentAttempts",
                column: "VersionId1");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId",
                table: "AssessmentResults",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AttemptId",
                table: "AssessmentResults",
                column: "AttemptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AttemptId1",
                table: "AssessmentResults",
                column: "AttemptId1");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_IsCalculated",
                table: "AssessmentResults",
                column: "IsCalculated");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_Code_CompanyId",
                table: "Assessments",
                columns: new[] { "Code", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CompanyId",
                table: "Assessments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_IsArchived",
                table: "Assessments",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_IsPublished",
                table: "Assessments",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentVersions_AssessmentId_VersionNumber",
                table: "AssessmentVersions",
                columns: new[] { "AssessmentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentVersions_AssessmentId1",
                table: "AssessmentVersions",
                column: "AssessmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentVersions_IsDraft",
                table: "AssessmentVersions",
                column: "IsDraft");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentVersions_IsPublished",
                table: "AssessmentVersions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Slug",
                table: "Companies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MathOperations_MathExpressionId",
                table: "MathOperations",
                column: "MathExpressionId");

            migrationBuilder.CreateIndex(
                name: "IX_MathVariables_MathExpressionId",
                table: "MathVariables",
                column: "MathExpressionId");

            migrationBuilder.CreateIndex(
                name: "IX_Options_QuestionId",
                table: "Options",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CalculationOutputKey",
                table: "Questions",
                column: "CalculationOutputKey");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Key",
                table: "Questions",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_MathExpressionExpression",
                table: "Questions",
                column: "MathExpressionExpression");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_StepId",
                table: "Questions",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_StepId1",
                table: "Questions",
                column: "StepId1");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Type",
                table: "Questions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Steps_VersionId_Order",
                table: "Steps",
                columns: new[] { "VersionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Steps_VersionId1",
                table: "Steps",
                column: "VersionId1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId1",
                table: "Users",
                column: "CompanyId1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Plan",
                table: "Users",
                column: "Plan");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRules_Field_Type",
                table: "ValidationRules",
                columns: new[] { "Field", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRules_QuestionId",
                table: "ValidationRules",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_VisibilityConditions_QuestionId",
                table: "VisibilityConditions",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "MathOperations");

            migrationBuilder.DropTable(
                name: "MathVariables");

            migrationBuilder.DropTable(
                name: "Options");

            migrationBuilder.DropTable(
                name: "ScoredAnswer");

            migrationBuilder.DropTable(
                name: "ValidationRules");

            migrationBuilder.DropTable(
                name: "VisibilityConditions");

            migrationBuilder.DropTable(
                name: "AssessmentResults");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "AssessmentAttempts");

            migrationBuilder.DropTable(
                name: "CalculationConfigs");

            migrationBuilder.DropTable(
                name: "MathExpressions");

            migrationBuilder.DropTable(
                name: "Steps");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AssessmentVersions");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
