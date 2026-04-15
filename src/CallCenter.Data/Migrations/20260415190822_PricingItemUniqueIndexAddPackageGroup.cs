using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class PricingItemUniqueIndexAddPackageGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServicePricingItems_PeriodId_ProductTypeId_ServiceId",
                table: "ServicePricingItems");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePricingItems_PeriodId_ProductTypeId_ServiceId_Packag~",
                table: "ServicePricingItems",
                columns: new[] { "PeriodId", "ProductTypeId", "ServiceId", "PackageGroupId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServicePricingItems_PeriodId_ProductTypeId_ServiceId_Packag~",
                table: "ServicePricingItems");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePricingItems_PeriodId_ProductTypeId_ServiceId",
                table: "ServicePricingItems",
                columns: new[] { "PeriodId", "ProductTypeId", "ServiceId" },
                unique: true);
        }
    }
}
