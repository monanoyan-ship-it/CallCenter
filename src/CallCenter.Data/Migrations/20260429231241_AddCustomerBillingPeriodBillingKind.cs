using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBillingPeriodBillingKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerBillingPeriods_CustomerId_Year_Month",
                table: "CustomerBillingPeriods");

            migrationBuilder.AddColumn<int>(
                name: "BillingKindId",
                table: "CustomerBillingPeriods",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // 2 = SalonPlatform — modül kalemi olan tahakkuklar; kalan 1 = CallCenter (varsayılan)
            migrationBuilder.Sql(@"
UPDATE ""CustomerBillingPeriods"" p
SET ""BillingKindId"" = 2
WHERE EXISTS (
  SELECT 1 FROM ""CustomerBillingPeriodModuleLines"" l
  WHERE l.""CustomerBillingPeriodId"" = p.""Id"");
");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillingPeriods_CustomerId_Year_Month_BillingKindId",
                table: "CustomerBillingPeriods",
                columns: new[] { "CustomerId", "Year", "Month", "BillingKindId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerBillingPeriods_CustomerId_Year_Month_BillingKindId",
                table: "CustomerBillingPeriods");

            migrationBuilder.DropColumn(
                name: "BillingKindId",
                table: "CustomerBillingPeriods");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillingPeriods_CustomerId_Year_Month",
                table: "CustomerBillingPeriods",
                columns: new[] { "CustomerId", "Year", "Month" },
                unique: true);
        }
    }
}
