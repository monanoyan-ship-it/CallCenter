using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonSupplierOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnSupplierOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnSupplierOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnSupplierOrders_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnSupplierOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnSupplierOrders_SlnSuppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "SlnSuppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlnSupplierOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierOrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnSupplierOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnSupplierOrderItems_SlnProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SlnProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlnSupplierOrderItems_SlnSupplierOrders_SupplierOrderId",
                        column: x => x.SupplierOrderId,
                        principalTable: "SlnSupplierOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnSupplierOrderItems_ProductId",
                table: "SlnSupplierOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnSupplierOrderItems_SupplierOrderId",
                table: "SlnSupplierOrderItems",
                column: "SupplierOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnSupplierOrders_CreatedByPersonnelId",
                table: "SlnSupplierOrders",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnSupplierOrders_CustomerId_OrderNo",
                table: "SlnSupplierOrders",
                columns: new[] { "CustomerId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnSupplierOrders_CustomerId_SupplierId_StatusId",
                table: "SlnSupplierOrders",
                columns: new[] { "CustomerId", "SupplierId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnSupplierOrders_SupplierId",
                table: "SlnSupplierOrders",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnSupplierOrderItems");

            migrationBuilder.DropTable(
                name: "SlnSupplierOrders");
        }
    }
}
