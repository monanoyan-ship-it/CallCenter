using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlnReviewPlatformUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnReviews"
                    ADD COLUMN IF NOT EXISTS "PlatformUserId" integer;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_SlnReviews_PlatformUserId"
                    ON "SlnReviews" ("PlatformUserId");
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'FK_SlnReviews_PlatformUsers_PlatformUserId'
                          AND table_name = 'SlnReviews'
                    ) THEN
                        ALTER TABLE "SlnReviews"
                            ADD CONSTRAINT "FK_SlnReviews_PlatformUsers_PlatformUserId"
                            FOREIGN KEY ("PlatformUserId") REFERENCES "PlatformUsers" ("Id") ON DELETE NO ACTION;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnReviews"
                    DROP CONSTRAINT IF EXISTS "FK_SlnReviews_PlatformUsers_PlatformUserId";
                """);
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_SlnReviews_PlatformUserId";
                """);
            migrationBuilder.Sql("""
                ALTER TABLE "SlnReviews" DROP COLUMN IF EXISTS "PlatformUserId";
                """);
        }
    }
}
