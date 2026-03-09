using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitSipAccountIntoGatewayAndLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Once SipLines tablosunu olustur
            migrationBuilder.CreateTable(
                name: "SipLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChannelNumber = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SipAccountId = table.Column<int>(type: "integer", nullable: false),
                    AssignedPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SipLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SipLines_CustomerPersonnel_AssignedPersonnelId",
                        column: x => x.AssignedPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SipLines_SipAccounts_SipAccountId",
                        column: x => x.SipAccountId,
                        principalTable: "SipAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SipLines_AssignedPersonnelId",
                table: "SipLines",
                column: "AssignedPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SipLines_SipAccountId",
                table: "SipLines",
                column: "SipAccountId");

            // 2. Mevcut SipAccounts verilerini SipLines'a tasi (data migration)
            migrationBuilder.Sql(@"
                INSERT INTO ""SipLines"" (""ChannelNumber"", ""Username"", ""Password"", ""Description"", ""IsActive"", ""CreatedAt"", ""SipAccountId"", ""AssignedPersonnelId"", ""AssignedAt"")
                SELECT 1, ""Username"", ""Password"", NULL, ""IsActive"", ""CreatedAt"", ""Id"", ""AssignedPersonnelId"", ""AssignedAt""
                FROM ""SipAccounts""
                WHERE ""Username"" IS NOT NULL AND ""Username"" <> '';
            ");

            // 3. Artik SipAccounts'tan eski kolonlari kaldir
            migrationBuilder.DropForeignKey(
                name: "FK_SipAccounts_CustomerPersonnel_AssignedPersonnelId",
                table: "SipAccounts");

            migrationBuilder.DropIndex(
                name: "IX_SipAccounts_AssignedPersonnelId",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "AssignedPersonnelId",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "SipAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SipLines");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "SipAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedPersonnelId",
                table: "SipAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "SipAccounts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "SipAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SipAccounts_AssignedPersonnelId",
                table: "SipAccounts",
                column: "AssignedPersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_SipAccounts_CustomerPersonnel_AssignedPersonnelId",
                table: "SipAccounts",
                column: "AssignedPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id");
        }
    }
}
