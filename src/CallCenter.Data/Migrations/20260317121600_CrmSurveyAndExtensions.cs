using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class CrmSurveyAndExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "CrmTickets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CrmContactTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmContactTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmContactTags_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmSurveys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmSurveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmSurveys_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmSurveys_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmTicketCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmTicketCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmTicketCategories_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmTicketComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmTicketComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmTicketComments_CrmTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "CrmTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmTicketComments_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmContactTagLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmContactTagLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmContactTagLinks_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmContactTagLinks_CrmContactTags_TagId",
                        column: x => x.TagId,
                        principalTable: "CrmContactTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmSurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurveyId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QuestionTypeId = table.Column<int>(type: "integer", nullable: false),
                    Options = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MinValue = table.Column<int>(type: "integer", nullable: true),
                    MaxValue = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmSurveyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmSurveyQuestions_CrmSurveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "CrmSurveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmSurveyResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<int>(type: "integer", nullable: false),
                    ContactId = table.Column<int>(type: "integer", nullable: true),
                    CallRecordId = table.Column<int>(type: "integer", nullable: true),
                    RespondentPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RespondentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OverallScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmSurveyResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmSurveyResponses_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmSurveyResponses_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmSurveyResponses_CrmSurveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "CrmSurveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmSurveyResponses_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmSurveyResponses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmSurveyAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResponseId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    AnswerText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AnswerScore = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmSurveyAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmSurveyAnswers_CrmSurveyQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "CrmSurveyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmSurveyAnswers_CrmSurveyResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "CrmSurveyResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmTickets_CategoryId",
                table: "CrmTickets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContactTagLinks_ContactId_TagId",
                table: "CrmContactTagLinks",
                columns: new[] { "ContactId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmContactTagLinks_TagId",
                table: "CrmContactTagLinks",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContactTags_CustomerId_Name",
                table: "CrmContactTags",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyAnswers_QuestionId",
                table: "CrmSurveyAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyAnswers_ResponseId_QuestionId",
                table: "CrmSurveyAnswers",
                columns: new[] { "ResponseId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyQuestions_SurveyId_SortOrder",
                table: "CrmSurveyQuestions",
                columns: new[] { "SurveyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyResponses_CallRecordId",
                table: "CrmSurveyResponses",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyResponses_ContactId",
                table: "CrmSurveyResponses",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyResponses_CreatedByPersonnelId",
                table: "CrmSurveyResponses",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyResponses_CustomerId",
                table: "CrmSurveyResponses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyResponses_SurveyId_CreatedAt",
                table: "CrmSurveyResponses",
                columns: new[] { "SurveyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveyResponses_Uid",
                table: "CrmSurveyResponses",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveys_CreatedByPersonnelId",
                table: "CrmSurveys",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveys_CustomerId_IsActive",
                table: "CrmSurveys",
                columns: new[] { "CustomerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmSurveys_Uid",
                table: "CrmSurveys",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmTicketCategories_CustomerId_Name",
                table: "CrmTicketCategories",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmTicketComments_CreatedByPersonnelId",
                table: "CrmTicketComments",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTicketComments_TicketId",
                table: "CrmTicketComments",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_CrmTickets_CrmTicketCategories_CategoryId",
                table: "CrmTickets",
                column: "CategoryId",
                principalTable: "CrmTicketCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrmTickets_CrmTicketCategories_CategoryId",
                table: "CrmTickets");

            migrationBuilder.DropTable(
                name: "CrmContactTagLinks");

            migrationBuilder.DropTable(
                name: "CrmSurveyAnswers");

            migrationBuilder.DropTable(
                name: "CrmTicketCategories");

            migrationBuilder.DropTable(
                name: "CrmTicketComments");

            migrationBuilder.DropTable(
                name: "CrmContactTags");

            migrationBuilder.DropTable(
                name: "CrmSurveyQuestions");

            migrationBuilder.DropTable(
                name: "CrmSurveyResponses");

            migrationBuilder.DropTable(
                name: "CrmSurveys");

            migrationBuilder.DropIndex(
                name: "IX_CrmTickets_CategoryId",
                table: "CrmTickets");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "CrmTickets");
        }
    }
}
