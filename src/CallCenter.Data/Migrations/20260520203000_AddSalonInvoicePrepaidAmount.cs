using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520203000_AddSalonInvoicePrepaidAmount")]
    public partial class AddSalonInvoicePrepaidAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnInvoices"
                ADD COLUMN IF NOT EXISTS "PrepaidAmount" numeric(18,2) NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnInvoices"
                DROP COLUMN IF EXISTS "PrepaidAmount";
                """);
        }
    }
}
