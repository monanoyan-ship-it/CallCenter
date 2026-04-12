using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class BranchActivationDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='ActivatedAt') THEN
                        ALTER TABLE "SlnBranches" ADD "ActivatedAt" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='DeactivatedAt') THEN
                        ALTER TABLE "SlnBranches" ADD "DeactivatedAt" timestamp with time zone;
                    END IF;
                    -- Mevcut aktif subelerin ActivatedAt'ini CreatedAt ile doldur
                    UPDATE "SlnBranches" SET "ActivatedAt" = "CreatedAt" WHERE "IsActive" = true AND "ActivatedAt" IS NULL;
                END $$;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ActivatedAt", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "DeactivatedAt", table: "SlnBranches");
        }
    }
}
