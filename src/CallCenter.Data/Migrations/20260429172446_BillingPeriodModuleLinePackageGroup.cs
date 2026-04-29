using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class BillingPeriodModuleLinePackageGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ModuleId",
                table: "CustomerBillingPeriodModuleLines",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "PackageGroupId",
                table: "CustomerBillingPeriodModuleLines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillingPeriodModuleLines_PackageGroupId",
                table: "CustomerBillingPeriodModuleLines",
                column: "PackageGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerBillingPeriodModuleLines_PackageGroupId",
                table: "CustomerBillingPeriodModuleLines");

            migrationBuilder.DropColumn(
                name: "PackageGroupId",
                table: "CustomerBillingPeriodModuleLines");

            migrationBuilder.AlterColumn<int>(
                name: "ModuleId",
                table: "CustomerBillingPeriodModuleLines",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
