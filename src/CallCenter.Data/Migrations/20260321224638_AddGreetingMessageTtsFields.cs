using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGreetingMessageTtsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTextToSpeech",
                table: "GreetingMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TextContent",
                table: "GreetingMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TtsLanguage",
                table: "GreetingMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TtsVoice",
                table: "GreetingMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTextToSpeech",
                table: "GreetingMessages");

            migrationBuilder.DropColumn(
                name: "TextContent",
                table: "GreetingMessages");

            migrationBuilder.DropColumn(
                name: "TtsLanguage",
                table: "GreetingMessages");

            migrationBuilder.DropColumn(
                name: "TtsVoice",
                table: "GreetingMessages");
        }
    }
}
