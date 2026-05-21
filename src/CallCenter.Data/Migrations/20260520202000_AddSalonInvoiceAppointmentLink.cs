using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520202000_AddSalonInvoiceAppointmentLink")]
    public partial class AddSalonInvoiceAppointmentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnInvoices"
                ADD COLUMN IF NOT EXISTS "SlnAppointmentId" integer;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SlnInvoices_SlnAppointmentId"
                ON "SlnInvoices" ("SlnAppointmentId")
                WHERE "SlnAppointmentId" IS NOT NULL AND "StatusId" <> 3;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_SlnInvoices_SlnAppointments_SlnAppointmentId'
                    ) THEN
                        ALTER TABLE "SlnInvoices"
                        ADD CONSTRAINT "FK_SlnInvoices_SlnAppointments_SlnAppointmentId"
                        FOREIGN KEY ("SlnAppointmentId")
                        REFERENCES "SlnAppointments" ("Id")
                        ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnInvoices"
                DROP CONSTRAINT IF EXISTS "FK_SlnInvoices_SlnAppointments_SlnAppointmentId";
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_SlnInvoices_SlnAppointmentId";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "SlnInvoices"
                DROP COLUMN IF EXISTS "SlnAppointmentId";
                """);
        }
    }
}
