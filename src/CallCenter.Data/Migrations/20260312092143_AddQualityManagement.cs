using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityChecklists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsScored = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ScoringMethodId = table.Column<int>(type: "integer", nullable: false),
                    MaxTotalPoints = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HideGroupNames = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityChecklists_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistId = table.Column<int>(type: "integer", nullable: false),
                    CallRecordId = table.Column<int>(type: "integer", nullable: false),
                    EvaluatorPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    EvaluatedPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxScore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ScorePercentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluationComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    YellowCardCount = table.Column<int>(type: "integer", nullable: false),
                    RedCardCount = table.Column<int>(type: "integer", nullable: false),
                    FormOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityEvaluations_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityEvaluations_CustomerPersonnel_EvaluatedPersonnelId",
                        column: x => x.EvaluatedPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityEvaluations_CustomerPersonnel_EvaluatorPersonnelId",
                        column: x => x.EvaluatorPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityEvaluations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityEvaluations_QualityChecklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "QualityChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChecklistId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ScoringTypeId = table.Column<int>(type: "integer", nullable: false),
                    WeightPoints = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: false),
                    PenaltyTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    HelpText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GroupName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShowScoreInput = table.Column<bool>(type: "boolean", nullable: false),
                    AllowComment = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityQuestions_QualityChecklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "QualityChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityScoreThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SuccessThreshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WarningThreshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ChecklistId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityScoreThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityScoreThresholds_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityScoreThresholds_QualityChecklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "QualityChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QualityAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    GivenPoints = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EarnedPoints = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AnswerText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecommendationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPenaltyApplied = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedPenaltyTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsNotApplicable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityAnswers_QualityEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "QualityEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityAnswers_QualityQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QualityQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityQuestionSubCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    WeightPoints = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityQuestionSubCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityQuestionSubCriteria_QualityQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QualityQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityAnswerSubCriteriaSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnswerId = table.Column<int>(type: "integer", nullable: false),
                    SubCriteriaId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SelectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityAnswerSubCriteriaSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityAnswerSubCriteriaSelections_QualityAnswers_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "QualityAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityAnswerSubCriteriaSelections_QualityQuestionSubCriter~",
                        column: x => x.SubCriteriaId,
                        principalTable: "QualityQuestionSubCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityAnswers_EvaluationId_QuestionId",
                table: "QualityAnswers",
                columns: new[] { "EvaluationId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityAnswers_QuestionId",
                table: "QualityAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAnswerSubCriteriaSelections_AnswerId_SubCriteriaId",
                table: "QualityAnswerSubCriteriaSelections",
                columns: new[] { "AnswerId", "SubCriteriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityAnswerSubCriteriaSelections_SubCriteriaId",
                table: "QualityAnswerSubCriteriaSelections",
                column: "SubCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecklists_CustomerId_Name",
                table: "QualityChecklists",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecklists_Uid",
                table: "QualityChecklists",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityEvaluations_CallRecordId",
                table: "QualityEvaluations",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityEvaluations_ChecklistId",
                table: "QualityEvaluations",
                column: "ChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityEvaluations_CustomerId_StatusId",
                table: "QualityEvaluations",
                columns: new[] { "CustomerId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityEvaluations_EvaluatedPersonnelId",
                table: "QualityEvaluations",
                column: "EvaluatedPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityEvaluations_EvaluatorPersonnelId",
                table: "QualityEvaluations",
                column: "EvaluatorPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityEvaluations_Uid",
                table: "QualityEvaluations",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityQuestions_ChecklistId_Order",
                table: "QualityQuestions",
                columns: new[] { "ChecklistId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityQuestionSubCriteria_QuestionId_Order",
                table: "QualityQuestionSubCriteria",
                columns: new[] { "QuestionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityScoreThresholds_ChecklistId",
                table: "QualityScoreThresholds",
                column: "ChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityScoreThresholds_CustomerId_ChecklistId",
                table: "QualityScoreThresholds",
                columns: new[] { "CustomerId", "ChecklistId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityAnswerSubCriteriaSelections");

            migrationBuilder.DropTable(
                name: "QualityScoreThresholds");

            migrationBuilder.DropTable(
                name: "QualityAnswers");

            migrationBuilder.DropTable(
                name: "QualityQuestionSubCriteria");

            migrationBuilder.DropTable(
                name: "QualityEvaluations");

            migrationBuilder.DropTable(
                name: "QualityQuestions");

            migrationBuilder.DropTable(
                name: "QualityChecklists");
        }
    }
}
