using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitLoyaltyPackageFromSessionService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnInvoiceItems_SlnClientPackages_ClientPackageId",
                table: "SlnInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_SlnGiftCards_CustomerId",
                table: "SlnGiftCards");

            migrationBuilder.RenameColumn(
                name: "ClientPackageId",
                table: "SlnInvoiceItems",
                newName: "LoyaltyPackagePurchaseId");

            migrationBuilder.RenameIndex(
                name: "IX_SlnInvoiceItems_ClientPackageId",
                table: "SlnInvoiceItems",
                newName: "IX_SlnInvoiceItems_LoyaltyPackagePurchaseId");

            migrationBuilder.AddColumn<int>(
                name: "SessionIndex",
                table: "SlnTreatmentRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSessions",
                table: "SlnTreatmentRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionCount",
                table: "SlnServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SlnLoyaltyPackageOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    BonusSessions = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnLoyaltyPackageOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageOffers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageOffers_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageOffers_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnLoyaltyPackagePurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    UsedSessions = table.Column<int>(type: "integer", nullable: false),
                    RemainingSessions = table.Column<int>(type: "integer", nullable: false),
                    SaleAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    SourceInvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SoldByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnLoyaltyPackagePurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackagePurchases_CustomerPersonnel_SoldByPersonne~",
                        column: x => x.SoldByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackagePurchases_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackagePurchases_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackagePurchases_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackagePurchases_SlnInvoiceItems_SourceInvoiceIte~",
                        column: x => x.SourceInvoiceItemId,
                        principalTable: "SlnInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackagePurchases_SlnInvoices_SourceInvoiceId",
                        column: x => x.SourceInvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackagePurchases_SlnLoyaltyPackageOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "SlnLoyaltyPackageOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlnLoyaltyPackageRedemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    SlnAppointmentId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnLoyaltyPackageRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageRedemptions_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageRedemptions_SlnAppointments_SlnAppointment~",
                        column: x => x.SlnAppointmentId,
                        principalTable: "SlnAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageRedemptions_SlnInvoiceItems_InvoiceItemId",
                        column: x => x.InvoiceItemId,
                        principalTable: "SlnInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageRedemptions_SlnInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageRedemptions_SlnLoyaltyPackagePurchases_Pur~",
                        column: x => x.PurchaseId,
                        principalTable: "SlnLoyaltyPackagePurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPackageRedemptions_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.Sql("""
                INSERT INTO "SlnLoyaltyPackageOffers"
                    ("Id", "CustomerId", "BranchId", "Name", "Description", "ServiceId", "TotalSessions", "BonusSessions", "Price", "ValidDays", "IsActive", "CreatedAt")
                SELECT
                    "Id", "CustomerId", NULL, "Name", "Description", "ServiceId", "TotalSessions", 0, "Price", "ValidDays", "IsActive", "CreatedAt"
                FROM "SlnPackageDefinitions";

                INSERT INTO "SlnLoyaltyPackagePurchases"
                    ("Id", "CustomerId", "OfferId", "SlnClientId", "BranchId", "TotalSessions", "UsedSessions", "RemainingSessions",
                     "SaleAmount", "PaidAmount", "SourceInvoiceId", "SourceInvoiceItemId", "ExpiresAt", "IsActive", "SoldByPersonnelId", "CreatedAt")
                SELECT
                    p."Id", p."CustomerId", p."PackageDefinitionId", p."SlnClientId", p."BranchId", p."TotalSessions", p."UsedSessions", p."RemainingSessions",
                    p."SaleAmount", p."PaidAmount", p."SourceInvoiceId", p."SourceInvoiceItemId", p."ExpiresAt", p."IsActive", p."SoldByPersonnelId", p."CreatedAt"
                FROM "SlnClientPackages" p
                WHERE p."SlnClientId" IS NOT NULL
                  AND EXISTS (SELECT 1 FROM "SlnLoyaltyPackageOffers" o WHERE o."Id" = p."PackageDefinitionId");

                INSERT INTO "SlnLoyaltyPackageRedemptions"
                    ("Id", "PurchaseId", "PersonnelId", "InvoiceId", "InvoiceItemId", "ServiceId", "SlnAppointmentId", "Notes", "UsedAt")
                SELECT
                    u."Id", u."ClientPackageId", u."PersonnelId", u."InvoiceId", u."InvoiceItemId", u."ServiceId", u."SlnAppointmentId", u."Notes", u."UsedAt"
                FROM "SlnPackageUsages" u
                WHERE EXISTS (SELECT 1 FROM "SlnLoyaltyPackagePurchases" p WHERE p."Id" = u."ClientPackageId");

                SELECT setval(pg_get_serial_sequence('"SlnLoyaltyPackageOffers"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM "SlnLoyaltyPackageOffers"), 0), 1), true);
                SELECT setval(pg_get_serial_sequence('"SlnLoyaltyPackagePurchases"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM "SlnLoyaltyPackagePurchases"), 0), 1), true);
                SELECT setval(pg_get_serial_sequence('"SlnLoyaltyPackageRedemptions"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM "SlnLoyaltyPackageRedemptions"), 0), 1), true);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageOffers_BranchId",
                table: "SlnLoyaltyPackageOffers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageOffers_CustomerId_BranchId",
                table: "SlnLoyaltyPackageOffers",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageOffers_CustomerId_ServiceId",
                table: "SlnLoyaltyPackageOffers",
                columns: new[] { "CustomerId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageOffers_ServiceId",
                table: "SlnLoyaltyPackageOffers",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackagePurchases_BranchId",
                table: "SlnLoyaltyPackagePurchases",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackagePurchases_CustomerId_BranchId",
                table: "SlnLoyaltyPackagePurchases",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackagePurchases_OfferId",
                table: "SlnLoyaltyPackagePurchases",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackagePurchases_SlnClientId",
                table: "SlnLoyaltyPackagePurchases",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackagePurchases_SoldByPersonnelId",
                table: "SlnLoyaltyPackagePurchases",
                column: "SoldByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackagePurchases_SourceInvoiceId",
                table: "SlnLoyaltyPackagePurchases",
                column: "SourceInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackagePurchases_SourceInvoiceItemId",
                table: "SlnLoyaltyPackagePurchases",
                column: "SourceInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageRedemptions_InvoiceId",
                table: "SlnLoyaltyPackageRedemptions",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageRedemptions_InvoiceItemId",
                table: "SlnLoyaltyPackageRedemptions",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageRedemptions_PersonnelId",
                table: "SlnLoyaltyPackageRedemptions",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageRedemptions_PurchaseId",
                table: "SlnLoyaltyPackageRedemptions",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageRedemptions_ServiceId",
                table: "SlnLoyaltyPackageRedemptions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPackageRedemptions_SlnAppointmentId",
                table: "SlnLoyaltyPackageRedemptions",
                column: "SlnAppointmentId");

            migrationBuilder.Sql("""
                UPDATE "SlnInvoiceItems" i
                SET "LoyaltyPackagePurchaseId" = NULL
                WHERE i."LoyaltyPackagePurchaseId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "SlnLoyaltyPackagePurchases" p
                      WHERE p."Id" = i."LoyaltyPackagePurchaseId"
                  );
                """);

            migrationBuilder.DropTable(
                name: "SlnPackageUsages");

            migrationBuilder.DropTable(
                name: "SlnClientPackages");

            migrationBuilder.DropTable(
                name: "SlnPackageDefinitions");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnInvoiceItems_SlnLoyaltyPackagePurchases_LoyaltyPackagePu~",
                table: "SlnInvoiceItems",
                column: "LoyaltyPackagePurchaseId",
                principalTable: "SlnLoyaltyPackagePurchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnInvoiceItems_SlnLoyaltyPackagePurchases_LoyaltyPackagePu~",
                table: "SlnInvoiceItems");

            migrationBuilder.DropTable(
                name: "SlnLoyaltyPackageRedemptions");

            migrationBuilder.DropTable(
                name: "SlnLoyaltyPackagePurchases");

            migrationBuilder.DropTable(
                name: "SlnLoyaltyPackageOffers");

            migrationBuilder.DropColumn(
                name: "SessionIndex",
                table: "SlnTreatmentRecords");

            migrationBuilder.DropColumn(
                name: "TotalSessions",
                table: "SlnTreatmentRecords");

            migrationBuilder.DropColumn(
                name: "SessionCount",
                table: "SlnServices");

            migrationBuilder.RenameColumn(
                name: "LoyaltyPackagePurchaseId",
                table: "SlnInvoiceItems",
                newName: "ClientPackageId");

            migrationBuilder.RenameIndex(
                name: "IX_SlnInvoiceItems_LoyaltyPackagePurchaseId",
                table: "SlnInvoiceItems",
                newName: "IX_SlnInvoiceItems_ClientPackageId");

            migrationBuilder.CreateTable(
                name: "SlnPackageDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    ValidDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPackageDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPackageDefinitions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPackageDefinitions_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnClientPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PackageDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: true),
                    SoldByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    SourceInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    SourceInvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingSessions = table.Column<int>(type: "integer", nullable: false),
                    SaleAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    UsedSessions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnClientPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_CustomerPersonnel_SoldByPersonnelId",
                        column: x => x.SoldByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_SlnInvoiceItems_SourceInvoiceItemId",
                        column: x => x.SourceInvoiceItemId,
                        principalTable: "SlnInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_SlnInvoices_SourceInvoiceId",
                        column: x => x.SourceInvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_SlnPackageDefinitions_PackageDefinitionId",
                        column: x => x.PackageDefinitionId,
                        principalTable: "SlnPackageDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlnPackageUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientPackageId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    SlnAppointmentId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPackageUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPackageUsages_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnPackageUsages_SlnAppointments_SlnAppointmentId",
                        column: x => x.SlnAppointmentId,
                        principalTable: "SlnAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnPackageUsages_SlnClientPackages_ClientPackageId",
                        column: x => x.ClientPackageId,
                        principalTable: "SlnClientPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPackageUsages_SlnInvoiceItems_InvoiceItemId",
                        column: x => x.InvoiceItemId,
                        principalTable: "SlnInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnPackageUsages_SlnInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnPackageUsages_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnGiftCards_CustomerId",
                table: "SlnGiftCards",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_BranchId",
                table: "SlnClientPackages",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_CustomerId_BranchId",
                table: "SlnClientPackages",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_PackageDefinitionId",
                table: "SlnClientPackages",
                column: "PackageDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_SlnClientId",
                table: "SlnClientPackages",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_SoldByPersonnelId",
                table: "SlnClientPackages",
                column: "SoldByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_SourceInvoiceId",
                table: "SlnClientPackages",
                column: "SourceInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_SourceInvoiceItemId",
                table: "SlnClientPackages",
                column: "SourceInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageDefinitions_CustomerId_ServiceId",
                table: "SlnPackageDefinitions",
                columns: new[] { "CustomerId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageDefinitions_ServiceId",
                table: "SlnPackageDefinitions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_ClientPackageId",
                table: "SlnPackageUsages",
                column: "ClientPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_InvoiceId",
                table: "SlnPackageUsages",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_InvoiceItemId",
                table: "SlnPackageUsages",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_PersonnelId",
                table: "SlnPackageUsages",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_ServiceId",
                table: "SlnPackageUsages",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_SlnAppointmentId",
                table: "SlnPackageUsages",
                column: "SlnAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnInvoiceItems_SlnClientPackages_ClientPackageId",
                table: "SlnInvoiceItems",
                column: "ClientPackageId",
                principalTable: "SlnClientPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
