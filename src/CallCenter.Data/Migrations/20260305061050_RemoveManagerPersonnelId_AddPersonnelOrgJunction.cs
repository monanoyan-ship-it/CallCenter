using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveManagerPersonnelId_AddPersonnelOrgJunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrganizationUnits_CustomerPersonnel_ManagerPersonne~",
                table: "CustomerOrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrganizationUnits_ManagerPersonnelId",
                table: "CustomerOrganizationUnits");

            migrationBuilder.DropColumn(
                name: "ManagerPersonnelId",
                table: "CustomerOrganizationUnits");

            migrationBuilder.CreateTable(
                name: "ConsentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ConsentTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    SubjectIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrivacyNoticeVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConsentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentRecords_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPersonnelOrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    OrganizationUnitId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPersonnelOrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnelOrganizationUnits_CustomerOrganizationUnit~",
                        column: x => x.OrganizationUnitId,
                        principalTable: "CustomerOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnelOrganizationUnits_CustomerPersonnel_Person~",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataBreaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    SeverityId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotificationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorityNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubjectsNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AffectedSubjectCount = table.Column<int>(type: "integer", nullable: true),
                    AffectedDataCategories = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CauseSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MeasuresTaken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReportedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReportedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataBreaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataBreaches_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DataDestructionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    MethodId = table.Column<int>(type: "integer", nullable: false),
                    DataCategory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DestroyedRecordCount = table.Column<int>(type: "integer", nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    DestroyedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDestructionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataDestructionLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DataInventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    DataCategory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DataSubjectGroup = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecipientGroup = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TransferCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    SecurityMeasures = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VerbisRegistrationNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataInventoryItems_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DataSubjectRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    RequestTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    RequesterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequesterIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequesterContact = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ResponseDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssignedToUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetentionPolicies_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_CustomerId_ConsentTypeId",
                table: "ConsentRecords",
                columns: new[] { "CustomerId", "ConsentTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_SubjectIdentifier",
                table: "ConsentRecords",
                column: "SubjectIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_Uid",
                table: "ConsentRecords",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelOrganizationUnits_OrganizationUnitId",
                table: "CustomerPersonnelOrganizationUnits",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelOrganizationUnits_PersonnelId_Organization~",
                table: "CustomerPersonnelOrganizationUnits",
                columns: new[] { "PersonnelId", "OrganizationUnitId" },
                unique: true);

            // Data migration: mevcut OrganizationUnitId degerlerini junction tablosuna kopyala
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerPersonnelOrganizationUnits"" (""PersonnelId"", ""OrganizationUnitId"")
                SELECT ""Id"", ""OrganizationUnitId""
                FROM ""CustomerPersonnel""
                WHERE ""OrganizationUnitId"" IS NOT NULL
                ON CONFLICT (""PersonnelId"", ""OrganizationUnitId"") DO NOTHING;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_DataBreaches_CustomerId",
                table: "DataBreaches",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DataBreaches_NotificationDeadline",
                table: "DataBreaches",
                column: "NotificationDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_DataBreaches_StatusId",
                table: "DataBreaches",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DataBreaches_Uid",
                table: "DataBreaches",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataDestructionLogs_CustomerId",
                table: "DataDestructionLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DataDestructionLogs_DestroyedAt",
                table: "DataDestructionLogs",
                column: "DestroyedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataInventoryItems_CustomerId",
                table: "DataInventoryItems",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_CustomerId_StatusId",
                table: "DataSubjectRequests",
                columns: new[] { "CustomerId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_Deadline",
                table: "DataSubjectRequests",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_Uid",
                table: "DataSubjectRequests",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetentionPolicies_CustomerId_CategoryId",
                table: "RetentionPolicies",
                columns: new[] { "CustomerId", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentRecords");

            migrationBuilder.DropTable(
                name: "CustomerPersonnelOrganizationUnits");

            migrationBuilder.DropTable(
                name: "DataBreaches");

            migrationBuilder.DropTable(
                name: "DataDestructionLogs");

            migrationBuilder.DropTable(
                name: "DataInventoryItems");

            migrationBuilder.DropTable(
                name: "DataSubjectRequests");

            migrationBuilder.DropTable(
                name: "RetentionPolicies");

            migrationBuilder.AddColumn<int>(
                name: "ManagerPersonnelId",
                table: "CustomerOrganizationUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizationUnits_ManagerPersonnelId",
                table: "CustomerOrganizationUnits",
                column: "ManagerPersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrganizationUnits_CustomerPersonnel_ManagerPersonne~",
                table: "CustomerOrganizationUnits",
                column: "ManagerPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
