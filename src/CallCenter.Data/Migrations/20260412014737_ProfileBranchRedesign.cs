using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfileBranchRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='Slug') THEN
                        ALTER TABLE "SlnBranches" ADD "Slug" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='GoogleMapsUrl') THEN
                        ALTER TABLE "SlnBranches" ADD "GoogleMapsUrl" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='CompanyTitle') THEN
                        ALTER TABLE "SlnBranches" ADD "CompanyTitle" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='TaxOffice') THEN
                        ALTER TABLE "SlnBranches" ADD "TaxOffice" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='TaxNumber') THEN
                        ALTER TABLE "SlnBranches" ADD "TaxNumber" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='MersisNo') THEN
                        ALTER TABLE "SlnBranches" ADD "MersisNo" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Customers' AND column_name='BillingType') THEN
                        ALTER TABLE "Customers" ADD "BillingType" integer NOT NULL DEFAULT 1;
                    END IF;
                END $$;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CompanyTitle", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "GoogleMapsUrl", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "MersisNo", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "Slug", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "TaxNumber", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "TaxOffice", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "BillingType", table: "Customers");
        }
    }
}
