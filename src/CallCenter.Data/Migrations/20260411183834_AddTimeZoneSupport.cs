using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: kolon zaten varsa atla
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PlatformUsers' AND column_name='TimeZone') THEN
                        ALTER TABLE "PlatformUsers" ADD COLUMN "TimeZone" text NOT NULL DEFAULT 'Europe/Istanbul';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Customers' AND column_name='TimeZone') THEN
                        ALTER TABLE "Customers" ADD COLUMN "TimeZone" text NOT NULL DEFAULT 'Europe/Istanbul';
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Customers");
        }
    }
}
