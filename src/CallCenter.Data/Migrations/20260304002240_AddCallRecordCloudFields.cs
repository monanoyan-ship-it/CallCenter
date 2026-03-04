using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCallRecordCloudFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudFileId",
                table: "CallRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudFileName",
                table: "CallRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudStorageConfigId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CloudUploadedAt",
                table: "CallRecords",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudFileId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CloudFileName",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CloudStorageConfigId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CloudUploadedAt",
                table: "CallRecords");
        }
    }
}
