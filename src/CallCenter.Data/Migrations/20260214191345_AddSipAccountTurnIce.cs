using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSipAccountTurnIce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StunServer",
                table: "SipAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TurnPassword",
                table: "SipAccounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TurnServer",
                table: "SipAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TurnUsername",
                table: "SipAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StunServer",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "TurnPassword",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "TurnServer",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "TurnUsername",
                table: "SipAccounts");
        }
    }
}
