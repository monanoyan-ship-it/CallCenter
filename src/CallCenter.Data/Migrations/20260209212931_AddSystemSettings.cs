using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Description", "Group", "IsSystem", "Key", "Value", "ValueType" },
                values: new object[,]
                {
                    { 1, "Uygulama adi", "general", true, "app.name", "Call Center", "string" },
                    { 2, "Varsayilan dil", "general", true, "app.language", "tr", "string" },
                    { 3, "Zaman dilimi", "general", true, "app.timezone", "Europe/Istanbul", "string" },
                    { 4, "Tarih formati", "general", true, "app.date_format", "dd.MM.yyyy", "string" },
                    { 10, "Maks hatali giris denemesi", "security", true, "security.max_login_attempts", "5", "int" },
                    { 11, "Hesap kilitleme suresi (dk)", "security", true, "security.lockout_minutes", "15", "int" },
                    { 12, "JWT token suresi (dk)", "security", true, "security.token_expire_minutes", "480", "int" },
                    { 13, "Minimum sifre uzunlugu", "security", true, "security.password_min_length", "6", "int" },
                    { 20, "Varsayilan SIP transport", "sip", true, "sip.default_transport", "UDP", "string" },
                    { 21, "SIP kayit suresi (sn)", "sip", true, "sip.registration_timeout", "3600", "int" },
                    { 22, "Keep-alive araligi (sn)", "sip", true, "sip.keep_alive_interval", "30", "int" },
                    { 30, "Bildirim sesi", "notification", false, "notification.sound_enabled", "true", "bool" },
                    { 31, "Masaustu bildirimi", "notification", false, "notification.desktop_enabled", "true", "bool" },
                    { 32, "Zil calma suresi (sn)", "notification", false, "notification.ring_duration", "30", "int" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
