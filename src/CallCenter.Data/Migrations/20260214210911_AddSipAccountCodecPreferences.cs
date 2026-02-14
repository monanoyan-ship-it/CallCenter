using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSipAccountCodecPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JitterBufferMaxMs",
                table: "SipAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "JitterBufferMinMs",
                table: "SipAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCodecs",
                table: "SipAccounts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JitterBufferMaxMs",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "JitterBufferMinMs",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "PreferredCodecs",
                table: "SipAccounts");
        }
    }
}
