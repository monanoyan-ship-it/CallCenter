using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyNoticeAndCallConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsentRecordId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsentStatusId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivacyNoticeDelivered",
                table: "CallRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PrivacyNoticeId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrivacyNotices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GreetingMessageId = table.Column<int>(type: "integer", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyNotices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrivacyNotices_GreetingMessages_GreetingMessageId",
                        column: x => x.GreetingMessageId,
                        principalTable: "GreetingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_ConsentRecordId",
                table: "CallRecords",
                column: "ConsentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_PrivacyNoticeId",
                table: "CallRecords",
                column: "PrivacyNoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyNotices_CustomerId_TypeId_IsActive",
                table: "PrivacyNotices",
                columns: new[] { "CustomerId", "TypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyNotices_GreetingMessageId",
                table: "PrivacyNotices",
                column: "GreetingMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyNotices_Uid",
                table: "PrivacyNotices",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CallRecords_ConsentRecords_ConsentRecordId",
                table: "CallRecords",
                column: "ConsentRecordId",
                principalTable: "ConsentRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CallRecords_PrivacyNotices_PrivacyNoticeId",
                table: "CallRecords",
                column: "PrivacyNoticeId",
                principalTable: "PrivacyNotices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallRecords_ConsentRecords_ConsentRecordId",
                table: "CallRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CallRecords_PrivacyNotices_PrivacyNoticeId",
                table: "CallRecords");

            migrationBuilder.DropTable(
                name: "PrivacyNotices");

            migrationBuilder.DropIndex(
                name: "IX_CallRecords_ConsentRecordId",
                table: "CallRecords");

            migrationBuilder.DropIndex(
                name: "IX_CallRecords_PrivacyNoticeId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "ConsentRecordId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "ConsentStatusId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "IsPrivacyNoticeDelivered",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "PrivacyNoticeId",
                table: "CallRecords");
        }
    }
}
