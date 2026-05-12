using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonResourcesAndCombos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BufferAfterMinutes",
                table: "SlnServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BufferBeforeMinutes",
                table: "SlnServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAddOn",
                table: "SlnServices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentServiceId",
                table: "SlnServices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrerequisiteNotes",
                table: "SlnServices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingMinutes",
                table: "SlnServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresConsultation",
                table: "SlnServices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPatchTest",
                table: "SlnServices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ComboId",
                table: "SlnAppointments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SlnResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ResourceKind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnResources_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnResources_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SlnServiceCombos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnServiceCombos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnServiceCombos_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnServiceResourceRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    ResourceId = table.Column<int>(type: "integer", nullable: false),
                    QuantityRequired = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnServiceResourceRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnServiceResourceRequirements_SlnResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "SlnResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServiceResourceRequirements_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnServiceComboItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComboId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnServiceComboItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnServiceComboItems_SlnServiceCombos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "SlnServiceCombos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServiceComboItems_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnServices_ParentServiceId",
                table: "SlnServices",
                column: "ParentServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnAppointments_ComboId",
                table: "SlnAppointments",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnResources_BranchId",
                table: "SlnResources",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnResources_CustomerId_BranchId_Name",
                table: "SlnResources",
                columns: new[] { "CustomerId", "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceComboItems_ComboId_ServiceId",
                table: "SlnServiceComboItems",
                columns: new[] { "ComboId", "ServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceComboItems_ServiceId",
                table: "SlnServiceComboItems",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceCombos_CustomerId_Name",
                table: "SlnServiceCombos",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceResourceRequirements_ResourceId",
                table: "SlnServiceResourceRequirements",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceResourceRequirements_ServiceId_ResourceId",
                table: "SlnServiceResourceRequirements",
                columns: new[] { "ServiceId", "ResourceId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnAppointments_SlnServiceCombos_ComboId",
                table: "SlnAppointments",
                column: "ComboId",
                principalTable: "SlnServiceCombos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnServices_SlnServices_ParentServiceId",
                table: "SlnServices",
                column: "ParentServiceId",
                principalTable: "SlnServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnAppointments_SlnServiceCombos_ComboId",
                table: "SlnAppointments");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnServices_SlnServices_ParentServiceId",
                table: "SlnServices");

            migrationBuilder.DropTable(
                name: "SlnServiceComboItems");

            migrationBuilder.DropTable(
                name: "SlnServiceResourceRequirements");

            migrationBuilder.DropTable(
                name: "SlnServiceCombos");

            migrationBuilder.DropTable(
                name: "SlnResources");

            migrationBuilder.DropIndex(
                name: "IX_SlnServices_ParentServiceId",
                table: "SlnServices");

            migrationBuilder.DropIndex(
                name: "IX_SlnAppointments_ComboId",
                table: "SlnAppointments");

            migrationBuilder.DropColumn(
                name: "BufferAfterMinutes",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "BufferBeforeMinutes",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "IsAddOn",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "ParentServiceId",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "PrerequisiteNotes",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "ProcessingMinutes",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "RequiresConsultation",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "RequiresPatchTest",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "ComboId",
                table: "SlnAppointments");
        }
    }
}
