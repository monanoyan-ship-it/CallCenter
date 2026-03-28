using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnPackageDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ValidDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PackageDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: true),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    UsedSessions = table.Column<int>(type: "integer", nullable: false),
                    RemainingSessions = table.Column<int>(type: "integer", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SoldByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        name: "FK_SlnClientPackages_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnClientPackages_SlnPackageDefinitions_PackageDefinitionId",
                        column: x => x.PackageDefinitionId,
                        principalTable: "SlnPackageDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnPackageUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientPackageId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
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
                        name: "FK_SlnPackageUsages_SlnClientPackages_ClientPackageId",
                        column: x => x.ClientPackageId,
                        principalTable: "SlnClientPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_CustomerId",
                table: "SlnClientPackages",
                column: "CustomerId");

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
                name: "IX_SlnPackageDefinitions_CustomerId",
                table: "SlnPackageDefinitions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageDefinitions_ServiceId",
                table: "SlnPackageDefinitions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_ClientPackageId",
                table: "SlnPackageUsages",
                column: "ClientPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_PersonnelId",
                table: "SlnPackageUsages",
                column: "PersonnelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnPackageUsages");

            migrationBuilder.DropTable(
                name: "SlnClientPackages");

            migrationBuilder.DropTable(
                name: "SlnPackageDefinitions");
        }
    }
}
