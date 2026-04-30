using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalonAccrualFromPeriodAndPlanCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExtraBranchMonthlyPrice",
                table: "ServicePricingPeriods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "ServicePricingPeriods"
                SET "ExtraBranchMonthlyPrice" = COALESCE((SELECT MAX("BranchPrice") FROM "SubscriptionPlans"), 0)
                WHERE "ExtraBranchMonthlyPrice" = 0;
                """);

            migrationBuilder.DropColumn(
                name: "BranchPrice",
                table: "SubscriptionPlans");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentOverride",
                table: "CustomerSubscriptions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.DropColumn(
                name: "NextBillingDate",
                table: "CustomerSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraBranchMonthlyPrice",
                table: "ServicePricingPeriods");

            migrationBuilder.DropColumn(
                name: "DiscountPercentOverride",
                table: "CustomerSubscriptions");

            migrationBuilder.AddColumn<decimal>(
                name: "BranchPrice",
                table: "SubscriptionPlans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextBillingDate",
                table: "CustomerSubscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
