using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistAppointmentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlnAppointmentId",
                table: "SlnWaitlistEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnWaitlistEntries_SlnAppointmentId",
                table: "SlnWaitlistEntries",
                column: "SlnAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnWaitlistEntries_SlnAppointments_SlnAppointmentId",
                table: "SlnWaitlistEntries",
                column: "SlnAppointmentId",
                principalTable: "SlnAppointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnWaitlistEntries_SlnAppointments_SlnAppointmentId",
                table: "SlnWaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_SlnWaitlistEntries_SlnAppointmentId",
                table: "SlnWaitlistEntries");

            migrationBuilder.DropColumn(
                name: "SlnAppointmentId",
                table: "SlnWaitlistEntries");
        }
    }
}
