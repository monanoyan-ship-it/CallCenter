using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlnBranchDecorationImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnBranches"
                    ADD COLUMN IF NOT EXISTS "CoverImageUrl" text,
                    ADD COLUMN IF NOT EXISTS "GalleryImagesJson" text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnBranches"
                    DROP COLUMN IF EXISTS "CoverImageUrl",
                    DROP COLUMN IF EXISTS "GalleryImagesJson";
                """);
        }
    }
}
