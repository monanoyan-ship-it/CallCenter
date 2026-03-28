using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCashClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnCashClosings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegisterId = table.Column<int>(type: "integer", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SystemTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CountedTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ClosedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnCashClosings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnCashClosings_CustomerPersonnel_ClosedByPersonnelId",
                        column: x => x.ClosedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnCashClosings_SlnCashRegisters_RegisterId",
                        column: x => x.RegisterId,
                        principalTable: "SlnCashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnCashClosings_ClosedByPersonnelId",
                table: "SlnCashClosings",
                column: "ClosedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnCashClosings_RegisterId",
                table: "SlnCashClosings",
                column: "RegisterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnCashClosings");
        }
    }
}
