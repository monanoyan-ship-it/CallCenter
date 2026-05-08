using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonnelWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "CustomerPersonnel"
                    ADD COLUMN IF NOT EXISTS "WorkingHoursJson" text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "CustomerPersonnel" DROP COLUMN IF EXISTS "WorkingHoursJson";
                """);
        }
    }
}
