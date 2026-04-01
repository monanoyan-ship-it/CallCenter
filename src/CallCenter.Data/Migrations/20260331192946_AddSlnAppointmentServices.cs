using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlnAppointmentServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnAppointments_SlnServices_ServiceId",
                table: "SlnAppointments");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceId",
                table: "SlnAppointments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "SlnAppointmentServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlnAppointmentId = table.Column<int>(type: "integer", nullable: false),
                    SlnServiceId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnAppointmentServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnAppointmentServices_SlnAppointments_SlnAppointmentId",
                        column: x => x.SlnAppointmentId,
                        principalTable: "SlnAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnAppointmentServices_SlnServices_SlnServiceId",
                        column: x => x.SlnServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnAppointmentServices_SlnAppointmentId",
                table: "SlnAppointmentServices",
                column: "SlnAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnAppointmentServices_SlnServiceId",
                table: "SlnAppointmentServices",
                column: "SlnServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnAppointments_SlnServices_ServiceId",
                table: "SlnAppointments",
                column: "ServiceId",
                principalTable: "SlnServices",
                principalColumn: "Id");

            // Mevcut ServiceId verilerini junction table'a tasi
            migrationBuilder.Sql(@"
                INSERT INTO ""SlnAppointmentServices"" (""SlnAppointmentId"", ""SlnServiceId"", ""SortOrder"")
                SELECT ""Id"", ""ServiceId"", 0
                FROM ""SlnAppointments""
                WHERE ""ServiceId"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnAppointments_SlnServices_ServiceId",
                table: "SlnAppointments");

            migrationBuilder.DropTable(
                name: "SlnAppointmentServices");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceId",
                table: "SlnAppointments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnAppointments_SlnServices_ServiceId",
                table: "SlnAppointments",
                column: "ServiceId",
                principalTable: "SlnServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
