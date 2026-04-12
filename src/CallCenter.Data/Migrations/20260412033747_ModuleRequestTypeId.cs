using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModuleRequestTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ModuleRequests' AND column_name='RequestTypeId') THEN
                        ALTER TABLE "ModuleRequests" ADD "RequestTypeId" integer NOT NULL DEFAULT 1;
                    END IF;
                END $$;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RequestTypeId", table: "ModuleRequests");
        }
    }
}
