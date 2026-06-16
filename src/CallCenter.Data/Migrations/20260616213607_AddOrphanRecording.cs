using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrphanRecording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrphanRecordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    CloudFileId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CloudFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PlatformFileId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AgentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MachineId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RetentionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedCallRecordId = table.Column<int>(type: "integer", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrphanRecordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrphanRecordings_CallRecords_ResolvedCallRecordId",
                        column: x => x.ResolvedCallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrphanRecordings_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrphanRecordings_CustomerId_IsResolved",
                table: "OrphanRecordings",
                columns: new[] { "CustomerId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_OrphanRecordings_ResolvedCallRecordId",
                table: "OrphanRecordings",
                column: "ResolvedCallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_OrphanRecordings_Uid",
                table: "OrphanRecordings",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrphanRecordings");
        }
    }
}
