using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class MembershipUsageAndPrepaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "SlnMembershipPlanServices");

            migrationBuilder.AddColumn<int>(
                name: "FreeCountPerMonth",
                table: "SlnMembershipPlanServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrepaid",
                table: "SlnAppointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTransactionId",
                table: "SlnAppointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrepaidAmount",
                table: "SlnAppointments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SlnMembershipUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    MembershipId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    UsedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnMembershipUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnMembershipUsages_SlnClientMemberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "SlnClientMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnMembershipUsages_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnMembershipUsages_MembershipId_ServiceId_Year_Month",
                table: "SlnMembershipUsages",
                columns: new[] { "MembershipId", "ServiceId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnMembershipUsages_ServiceId",
                table: "SlnMembershipUsages",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnMembershipUsages");

            migrationBuilder.DropColumn(
                name: "FreeCountPerMonth",
                table: "SlnMembershipPlanServices");

            migrationBuilder.DropColumn(
                name: "IsPrepaid",
                table: "SlnAppointments");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "SlnAppointments");

            migrationBuilder.DropColumn(
                name: "PrepaidAmount",
                table: "SlnAppointments");

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "SlnMembershipPlanServices",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
