using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceDefinitionUseTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerServiceSubscriptions_ServiceDefinitions_ServiceDefi~",
                table: "CustomerServiceSubscriptions");

            migrationBuilder.DropTable(
                name: "ServiceDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerServiceSubscriptions_ServiceDefinitionId",
                table: "CustomerServiceSubscriptions");

            migrationBuilder.RenameColumn(
                name: "ServiceDefinitionId",
                table: "CustomerServiceSubscriptions",
                newName: "ServiceTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerServiceSubscriptions_CustomerId_ServiceDefinitionId",
                table: "CustomerServiceSubscriptions",
                newName: "IX_CustomerServiceSubscriptions_CustomerId_ServiceTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ServiceTypeId",
                table: "CustomerServiceSubscriptions",
                newName: "ServiceDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerServiceSubscriptions_CustomerId_ServiceTypeId",
                table: "CustomerServiceSubscriptions",
                newName: "IX_CustomerServiceSubscriptions_CustomerId_ServiceDefinitionId");

            migrationBuilder.CreateTable(
                name: "ServiceDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DefaultPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDefinitions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ServiceDefinitions",
                columns: new[] { "Id", "CategoryId", "Code", "CreatedAt", "DefaultPrice", "Description", "IsActive", "Name", "SortOrder", "Uid" },
                values: new object[,]
                {
                    { 1, 1, "CALL_DIST", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0m, null, true, "Cagri Dagitimi", 1, new Guid("10000000-0000-0000-0000-000000000001") },
                    { 2, 1, "CALL_REC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0m, null, true, "Cagri Kaydi", 2, new Guid("10000000-0000-0000-0000-000000000002") },
                    { 3, 1, "VOICE_REC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0m, null, true, "Ses Kayitlari", 3, new Guid("10000000-0000-0000-0000-000000000003") },
                    { 4, 2, "IVR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 500m, null, true, "Sesli Yonlendirme (IVR)", 4, new Guid("10000000-0000-0000-0000-000000000004") },
                    { 5, 2, "QUALITY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 300m, null, true, "Kalite Yonetimi", 5, new Guid("10000000-0000-0000-0000-000000000005") },
                    { 6, 2, "CRM_INT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 400m, null, true, "CRM Entegrasyonu", 6, new Guid("10000000-0000-0000-0000-000000000006") },
                    { 7, 2, "CAMPAIGN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 350m, null, true, "Kampanya Modulu", 7, new Guid("10000000-0000-0000-0000-000000000007") },
                    { 8, 2, "REPORTING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 250m, null, true, "Gelismis Raporlama", 8, new Guid("10000000-0000-0000-0000-000000000008") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceSubscriptions_ServiceDefinitionId",
                table: "CustomerServiceSubscriptions",
                column: "ServiceDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinitions_Code",
                table: "ServiceDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinitions_Uid",
                table: "ServiceDefinitions",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerServiceSubscriptions_ServiceDefinitions_ServiceDefi~",
                table: "CustomerServiceSubscriptions",
                column: "ServiceDefinitionId",
                principalTable: "ServiceDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
