using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUserTypeWithFixedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. CustomerRoleId ekle (default 3 = Operator)
            migrationBuilder.AddColumn<int>(
                name: "CustomerRoleId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            // 2. Backfill: IsCustomerAdmin=true → FirmaAdmin(1), digerleri → Operator(3)
            migrationBuilder.Sql(
                """
                UPDATE "CustomerPersonnel" SET "CustomerRoleId" = 1 WHERE "IsCustomerAdmin" = true;
                UPDATE "CustomerPersonnel" SET "CustomerRoleId" = 3 WHERE "IsCustomerAdmin" = false OR "IsCustomerAdmin" IS NULL;
                """);

            // 3. Customers.MaxUsers ekle
            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // 4. UserTypeId FK kaldir
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_CustomerUserTypes_UserTypeId",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_UserTypeId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "UserTypeId",
                table: "CustomerPersonnel");

            // 5. CustomerUserTypePermissions tablosunu drop
            migrationBuilder.DropTable(
                name: "CustomerUserTypePermissions");

            // 6. CustomerUserTypes tablosunu drop
            migrationBuilder.DropTable(
                name: "CustomerUserTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerRoleId",
                table: "CustomerPersonnel");

            migrationBuilder.AddColumn<int>(
                name: "UserTypeId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerUserTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CanApprove = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageSubordinates = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUserTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerUserTypes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerUserTypePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserTypeId = table.Column<int>(type: "integer", nullable: false),
                    PermissionTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUserTypePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerUserTypePermissions_CustomerUserTypes_UserTypeId",
                        column: x => x.UserTypeId,
                        principalTable: "CustomerUserTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_UserTypeId",
                table: "CustomerPersonnel",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserTypePermissions_UserTypeId_PermissionTypeId",
                table: "CustomerUserTypePermissions",
                columns: new[] { "UserTypeId", "PermissionTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserTypes_CustomerId_Name",
                table: "CustomerUserTypes",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserTypes_Uid",
                table: "CustomerUserTypes",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_CustomerUserTypes_UserTypeId",
                table: "CustomerPersonnel",
                column: "UserTypeId",
                principalTable: "CustomerUserTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
