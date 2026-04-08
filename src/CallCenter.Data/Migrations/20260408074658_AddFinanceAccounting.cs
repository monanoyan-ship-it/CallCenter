using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "SlnServices",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "SlnProducts",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotal",
                table: "SlnInvoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SlnInvoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SlnInvoiceItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "SlnInvoiceItems",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByPersonnelId",
                table: "SlnExpenses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentRef",
                table: "SlnExpenses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "SlnExpenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SlnExpenses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SlnCashOpenings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegisterId = table.Column<int>(type: "integer", nullable: false),
                    OpeningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCarriedForward = table.Column<bool>(type: "boolean", nullable: false),
                    OpenedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnCashOpenings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnCashOpenings_CustomerPersonnel_OpenedByPersonnelId",
                        column: x => x.OpenedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnCashOpenings_SlnCashRegisters_RegisterId",
                        column: x => x.RegisterId,
                        principalTable: "SlnCashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnClientLedgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RunningBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnClientLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnClientLedgers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnClientLedgers_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnClientLedgers_SlnInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlnInvoicePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PosDeviceId = table.Column<int>(type: "integer", nullable: true),
                    GiftCardId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnInvoicePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnInvoicePayments_SlnInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnInvoicePayments_SlnPosDevices_PosDeviceId",
                        column: x => x.PosDeviceId,
                        principalTable: "SlnPosDevices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlnInvoiceRefunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundMethodId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CashTransactionId = table.Column<int>(type: "integer", nullable: true),
                    RefundDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnInvoiceRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnInvoiceRefunds_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnInvoiceRefunds_SlnInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnCashOpenings_OpenedByPersonnelId",
                table: "SlnCashOpenings",
                column: "OpenedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnCashOpenings_RegisterId_OpeningDate",
                table: "SlnCashOpenings",
                columns: new[] { "RegisterId", "OpeningDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLedgers_CustomerId_SlnClientId",
                table: "SlnClientLedgers",
                columns: new[] { "CustomerId", "SlnClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLedgers_InvoiceId",
                table: "SlnClientLedgers",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLedgers_SlnClientId",
                table: "SlnClientLedgers",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoicePayments_InvoiceId",
                table: "SlnInvoicePayments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoicePayments_PosDeviceId",
                table: "SlnInvoicePayments",
                column: "PosDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoiceRefunds_InvoiceId",
                table: "SlnInvoiceRefunds",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoiceRefunds_PersonnelId",
                table: "SlnInvoiceRefunds",
                column: "PersonnelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnCashOpenings");

            migrationBuilder.DropTable(
                name: "SlnClientLedgers");

            migrationBuilder.DropTable(
                name: "SlnInvoicePayments");

            migrationBuilder.DropTable(
                name: "SlnInvoiceRefunds");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "SlnServices");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "SlnProducts");

            migrationBuilder.DropColumn(
                name: "GrandTotal",
                table: "SlnInvoices");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SlnInvoices");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SlnInvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "SlnInvoiceItems");

            migrationBuilder.DropColumn(
                name: "ApprovedByPersonnelId",
                table: "SlnExpenses");

            migrationBuilder.DropColumn(
                name: "DocumentRef",
                table: "SlnExpenses");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "SlnExpenses");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SlnExpenses");
        }
    }
}
