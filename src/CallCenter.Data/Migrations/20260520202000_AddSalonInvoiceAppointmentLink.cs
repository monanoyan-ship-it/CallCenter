using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonInvoiceAppointmentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlnAppointmentId",
                table: "SlnInvoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoices_SlnAppointmentId",
                table: "SlnInvoices",
                column: "SlnAppointmentId",
                unique: true,
                filter: "\"SlnAppointmentId\" IS NOT NULL AND \"StatusId\" <> 3");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnInvoices_SlnAppointments_SlnAppointmentId",
                table: "SlnInvoices",
                column: "SlnAppointmentId",
                principalTable: "SlnAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnInvoices_SlnAppointments_SlnAppointmentId",
                table: "SlnInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SlnInvoices_SlnAppointmentId",
                table: "SlnInvoices");

            migrationBuilder.DropColumn(
                name: "SlnAppointmentId",
                table: "SlnInvoices");
        }
    }
}
