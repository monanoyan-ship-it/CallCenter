using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAnchorDayAndStatusId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingAnchorDay",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ServiceAmount",
                table: "CustomerBillingPeriods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "CustomerBillingPeriods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Mevcut musterilere BillingAnchorDay ata (donem kaydi olanlara CreatedAt gununu)
            migrationBuilder.Sql(@"
                UPDATE ""Customers"" SET ""BillingAnchorDay"" = EXTRACT(DAY FROM ""CreatedAt"")::int
                WHERE ""Id"" IN (SELECT DISTINCT ""CustomerId"" FROM ""CustomerBillingPeriods"");
            ");

            // Mevcut donemlere StatusId ata: IsPaid=true -> 3(Paid), IsPaid=false -> 1(Draft)
            migrationBuilder.Sql(@"
                UPDATE ""CustomerBillingPeriods"" SET ""StatusId"" = CASE WHEN ""IsPaid"" = true THEN 3 ELSE 1 END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAnchorDay",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "CustomerBillingPeriods");

            migrationBuilder.AlterColumn<decimal>(
                name: "ServiceAmount",
                table: "CustomerBillingPeriods",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
