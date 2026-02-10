using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizationUnitId",
                table: "SipAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationUnitId",
                table: "Queues",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanApprove",
                table: "CustomerUserTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageSubordinates",
                table: "CustomerUserTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "CustomerUserTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationUnitId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportsToPersonnelId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerOrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    ManagerPersonnelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerOrganizationUnits_CustomerOrganizationUnits_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CustomerOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrganizationUnits_CustomerPersonnel_ManagerPersonne~",
                        column: x => x.ManagerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerOrganizationUnits_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SipAccounts_OrganizationUnitId",
                table: "SipAccounts",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_OrganizationUnitId",
                table: "Queues",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_OrganizationUnitId",
                table: "CustomerPersonnel",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_ReportsToPersonnelId",
                table: "CustomerPersonnel",
                column: "ReportsToPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizationUnits_CustomerId_Name_ParentId",
                table: "CustomerOrganizationUnits",
                columns: new[] { "CustomerId", "Name", "ParentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizationUnits_ManagerPersonnelId",
                table: "CustomerOrganizationUnits",
                column: "ManagerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizationUnits_ParentId",
                table: "CustomerOrganizationUnits",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizationUnits_Uid",
                table: "CustomerOrganizationUnits",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_CustomerOrganizationUnits_OrganizationUni~",
                table: "CustomerPersonnel",
                column: "OrganizationUnitId",
                principalTable: "CustomerOrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_CustomerPersonnel_ReportsToPersonnelId",
                table: "CustomerPersonnel",
                column: "ReportsToPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Queues_CustomerOrganizationUnits_OrganizationUnitId",
                table: "Queues",
                column: "OrganizationUnitId",
                principalTable: "CustomerOrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SipAccounts_CustomerOrganizationUnits_OrganizationUnitId",
                table: "SipAccounts",
                column: "OrganizationUnitId",
                principalTable: "CustomerOrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_CustomerOrganizationUnits_OrganizationUni~",
                table: "CustomerPersonnel");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_CustomerPersonnel_ReportsToPersonnelId",
                table: "CustomerPersonnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Queues_CustomerOrganizationUnits_OrganizationUnitId",
                table: "Queues");

            migrationBuilder.DropForeignKey(
                name: "FK_SipAccounts_CustomerOrganizationUnits_OrganizationUnitId",
                table: "SipAccounts");

            migrationBuilder.DropTable(
                name: "CustomerOrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_SipAccounts_OrganizationUnitId",
                table: "SipAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Queues_OrganizationUnitId",
                table: "Queues");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_OrganizationUnitId",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_ReportsToPersonnelId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "OrganizationUnitId",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "OrganizationUnitId",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "CanApprove",
                table: "CustomerUserTypes");

            migrationBuilder.DropColumn(
                name: "CanManageSubordinates",
                table: "CustomerUserTypes");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "CustomerUserTypes");

            migrationBuilder.DropColumn(
                name: "OrganizationUnitId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "ReportsToPersonnelId",
                table: "CustomerPersonnel");
        }
    }
}
