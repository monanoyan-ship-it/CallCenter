using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBillingPeriodModuleLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerBillingPeriodModuleLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerBillingPeriodId = table.Column<int>(type: "integer", nullable: false),
                    CustomerPortalModuleId = table.Column<int>(type: "integer", nullable: true),
                    ModuleId = table.Column<int>(type: "integer", nullable: false),
                    ModuleDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MonthlyUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBillingPeriodModuleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBillingPeriodModuleLines_CustomerBillingPeriods_Cus~",
                        column: x => x.CustomerBillingPeriodId,
                        principalTable: "CustomerBillingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerBillingPeriodModuleLines_CustomerPortalModules_Cust~",
                        column: x => x.CustomerPortalModuleId,
                        principalTable: "CustomerPortalModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillingPeriodModuleLines_CustomerBillingPeriodId",
                table: "CustomerBillingPeriodModuleLines",
                column: "CustomerBillingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillingPeriodModuleLines_CustomerPortalModuleId",
                table: "CustomerBillingPeriodModuleLines",
                column: "CustomerPortalModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerBillingPeriodModuleLines");
        }
    }
}
