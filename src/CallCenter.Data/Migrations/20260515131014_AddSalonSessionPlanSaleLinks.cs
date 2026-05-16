using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonSessionPlanSaleLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnClientPackages_SlnPackageDefinitions_PackageDefinitionId",
                table: "SlnClientPackages");

            migrationBuilder.DropIndex(
                name: "IX_SlnPackageDefinitions_CustomerId",
                table: "SlnPackageDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_SlnClientPackages_CustomerId",
                table: "SlnClientPackages");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "SlnPackageUsages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceItemId",
                table: "SlnPackageUsages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "SlnPackageUsages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlnAppointmentId",
                table: "SlnPackageUsages",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "SlnPackageDefinitions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "ClientPackageId",
                table: "SlnInvoiceItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSessionUsage",
                table: "SlnInvoiceItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "PaidAmount",
                table: "SlnClientPackages",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnClientPackages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleAmount",
                table: "SlnClientPackages",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SourceInvoiceId",
                table: "SlnClientPackages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceInvoiceItemId",
                table: "SlnClientPackages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_InvoiceId",
                table: "SlnPackageUsages",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_InvoiceItemId",
                table: "SlnPackageUsages",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_ServiceId",
                table: "SlnPackageUsages",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageUsages_SlnAppointmentId",
                table: "SlnPackageUsages",
                column: "SlnAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageDefinitions_CustomerId_ServiceId",
                table: "SlnPackageDefinitions",
                columns: new[] { "CustomerId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoiceItems_ClientPackageId",
                table: "SlnInvoiceItems",
                column: "ClientPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_BranchId",
                table: "SlnClientPackages",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_CustomerId_BranchId",
                table: "SlnClientPackages",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_SourceInvoiceId",
                table: "SlnClientPackages",
                column: "SourceInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_SourceInvoiceItemId",
                table: "SlnClientPackages",
                column: "SourceInvoiceItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnClientPackages_SlnBranches_BranchId",
                table: "SlnClientPackages",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnClientPackages_SlnInvoiceItems_SourceInvoiceItemId",
                table: "SlnClientPackages",
                column: "SourceInvoiceItemId",
                principalTable: "SlnInvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnClientPackages_SlnInvoices_SourceInvoiceId",
                table: "SlnClientPackages",
                column: "SourceInvoiceId",
                principalTable: "SlnInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnClientPackages_SlnPackageDefinitions_PackageDefinitionId",
                table: "SlnClientPackages",
                column: "PackageDefinitionId",
                principalTable: "SlnPackageDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnInvoiceItems_SlnClientPackages_ClientPackageId",
                table: "SlnInvoiceItems",
                column: "ClientPackageId",
                principalTable: "SlnClientPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnPackageUsages_SlnAppointments_SlnAppointmentId",
                table: "SlnPackageUsages",
                column: "SlnAppointmentId",
                principalTable: "SlnAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnPackageUsages_SlnInvoiceItems_InvoiceItemId",
                table: "SlnPackageUsages",
                column: "InvoiceItemId",
                principalTable: "SlnInvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnPackageUsages_SlnInvoices_InvoiceId",
                table: "SlnPackageUsages",
                column: "InvoiceId",
                principalTable: "SlnInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnPackageUsages_SlnServices_ServiceId",
                table: "SlnPackageUsages",
                column: "ServiceId",
                principalTable: "SlnServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnClientPackages_SlnBranches_BranchId",
                table: "SlnClientPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnClientPackages_SlnInvoiceItems_SourceInvoiceItemId",
                table: "SlnClientPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnClientPackages_SlnInvoices_SourceInvoiceId",
                table: "SlnClientPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnClientPackages_SlnPackageDefinitions_PackageDefinitionId",
                table: "SlnClientPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnInvoiceItems_SlnClientPackages_ClientPackageId",
                table: "SlnInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnPackageUsages_SlnAppointments_SlnAppointmentId",
                table: "SlnPackageUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnPackageUsages_SlnInvoiceItems_InvoiceItemId",
                table: "SlnPackageUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnPackageUsages_SlnInvoices_InvoiceId",
                table: "SlnPackageUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnPackageUsages_SlnServices_ServiceId",
                table: "SlnPackageUsages");

            migrationBuilder.DropIndex(
                name: "IX_SlnPackageUsages_InvoiceId",
                table: "SlnPackageUsages");

            migrationBuilder.DropIndex(
                name: "IX_SlnPackageUsages_InvoiceItemId",
                table: "SlnPackageUsages");

            migrationBuilder.DropIndex(
                name: "IX_SlnPackageUsages_ServiceId",
                table: "SlnPackageUsages");

            migrationBuilder.DropIndex(
                name: "IX_SlnPackageUsages_SlnAppointmentId",
                table: "SlnPackageUsages");

            migrationBuilder.DropIndex(
                name: "IX_SlnPackageDefinitions_CustomerId_ServiceId",
                table: "SlnPackageDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_SlnInvoiceItems_ClientPackageId",
                table: "SlnInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_SlnClientPackages_BranchId",
                table: "SlnClientPackages");

            migrationBuilder.DropIndex(
                name: "IX_SlnClientPackages_CustomerId_BranchId",
                table: "SlnClientPackages");

            migrationBuilder.DropIndex(
                name: "IX_SlnClientPackages_SourceInvoiceId",
                table: "SlnClientPackages");

            migrationBuilder.DropIndex(
                name: "IX_SlnClientPackages_SourceInvoiceItemId",
                table: "SlnClientPackages");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "SlnPackageUsages");

            migrationBuilder.DropColumn(
                name: "InvoiceItemId",
                table: "SlnPackageUsages");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "SlnPackageUsages");

            migrationBuilder.DropColumn(
                name: "SlnAppointmentId",
                table: "SlnPackageUsages");

            migrationBuilder.DropColumn(
                name: "ClientPackageId",
                table: "SlnInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsSessionUsage",
                table: "SlnInvoiceItems");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnClientPackages");

            migrationBuilder.DropColumn(
                name: "SaleAmount",
                table: "SlnClientPackages");

            migrationBuilder.DropColumn(
                name: "SourceInvoiceId",
                table: "SlnClientPackages");

            migrationBuilder.DropColumn(
                name: "SourceInvoiceItemId",
                table: "SlnClientPackages");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "SlnPackageDefinitions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PaidAmount",
                table: "SlnClientPackages",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_SlnPackageDefinitions_CustomerId",
                table: "SlnPackageDefinitions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPackages_CustomerId",
                table: "SlnClientPackages",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnClientPackages_SlnPackageDefinitions_PackageDefinitionId",
                table: "SlnClientPackages",
                column: "PackageDefinitionId",
                principalTable: "SlnPackageDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
