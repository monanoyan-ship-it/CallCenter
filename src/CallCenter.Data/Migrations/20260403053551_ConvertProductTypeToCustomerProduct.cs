using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertProductTypeToCustomerProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Yeni tablo olustur
            migrationBuilder.CreateTable(
                name: "CustomerProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProductTypeId = table.Column<int>(type: "integer", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerProducts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProducts_CustomerId_ProductTypeId",
                table: "CustomerProducts",
                columns: new[] { "CustomerId", "ProductTypeId" },
                unique: true);

            // 2. Data migration: mevcut veriden CustomerProducts tabloya aktar
            // ProductTypeId=1 (CallCenter) -> tek satir
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerProducts"" (""CustomerId"", ""ProductTypeId"", ""MonthlyPrice"", ""IsActive"", ""CreatedAt"")
                SELECT ""Id"", 1, ""MonthlyUnitPrice"", true, now()
                FROM ""Customers""
                WHERE ""ProductTypeId"" = 1;
            ");

            // ProductTypeId=2 (Salon) -> tek satir
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerProducts"" (""CustomerId"", ""ProductTypeId"", ""MonthlyPrice"", ""IsActive"", ""CreatedAt"")
                SELECT ""Id"", 2, ""MonthlyUnitPrice"", true, now()
                FROM ""Customers""
                WHERE ""ProductTypeId"" = 2;
            ");

            // ProductTypeId=3 (Both) -> iki satir: CC fiyatli + Salon 0
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerProducts"" (""CustomerId"", ""ProductTypeId"", ""MonthlyPrice"", ""IsActive"", ""CreatedAt"")
                SELECT ""Id"", 1, ""MonthlyUnitPrice"", true, now()
                FROM ""Customers""
                WHERE ""ProductTypeId"" = 3;
            ");
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerProducts"" (""CustomerId"", ""ProductTypeId"", ""MonthlyPrice"", ""IsActive"", ""CreatedAt"")
                SELECT ""Id"", 2, 0, true, now()
                FROM ""Customers""
                WHERE ""ProductTypeId"" = 3;
            ");

            // ProductTypeId=0 veya null (tanimlanmamis) -> CC olarak ata
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerProducts"" (""CustomerId"", ""ProductTypeId"", ""MonthlyPrice"", ""IsActive"", ""CreatedAt"")
                SELECT ""Id"", 1, ""MonthlyUnitPrice"", true, now()
                FROM ""Customers""
                WHERE ""ProductTypeId"" = 0 OR ""ProductTypeId"" IS NULL;
            ");

            // 3. Eski kolonlari sil
            migrationBuilder.DropColumn(
                name: "MonthlyUnitPrice",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                table: "Customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductTypeId",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyUnitPrice",
                table: "Customers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Geri al: CustomerProducts'tan Customers'a aktar
            migrationBuilder.Sql(@"
                UPDATE ""Customers"" c
                SET ""ProductTypeId"" = COALESCE((
                    SELECT CASE WHEN COUNT(*) > 1 THEN 3 ELSE MIN(cp.""ProductTypeId"") END
                    FROM ""CustomerProducts"" cp WHERE cp.""CustomerId"" = c.""Id"" AND cp.""IsActive""
                ), 1),
                ""MonthlyUnitPrice"" = COALESCE((
                    SELECT SUM(cp.""MonthlyPrice"") FROM ""CustomerProducts"" cp WHERE cp.""CustomerId"" = c.""Id"" AND cp.""IsActive""
                ), 0);
            ");

            migrationBuilder.DropTable(
                name: "CustomerProducts");
        }
    }
}
