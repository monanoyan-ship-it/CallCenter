using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordingStoragePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SaveRecordingToOwnStorage",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SaveRecordingToPlatform",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PlatformFileId",
                table: "CallRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlatformUploadedAt",
                table: "CallRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Description", "Group", "IsSystem", "Key", "Value", "ValueType" },
                values: new object[,]
                {
                    { 40, "Platform depolamasi aktif mi", "storage", true, "storage.platform_enabled", "false", "bool" },
                    { 41, "StorageProviders ID", "storage", true, "storage.platform_provider_type_id", "0", "int" },
                    { 42, "Sifrelenmis kimlik bilgileri (JSON)", "storage", true, "storage.platform_credentials", "", "encrypted_json" },
                    { 43, "Temel yol", "storage", true, "storage.platform_base_path", "/recordings/", "string" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DropColumn(
                name: "SaveRecordingToOwnStorage",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SaveRecordingToPlatform",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PlatformFileId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "PlatformUploadedAt",
                table: "CallRecords");
        }
    }
}
