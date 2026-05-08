using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformUserAuthTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "PlatformUsers"
                    ADD COLUMN IF NOT EXISTS "EmailVerificationToken" text,
                    ADD COLUMN IF NOT EXISTS "EmailVerificationSentAt" timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS "PasswordResetToken" text,
                    ADD COLUMN IF NOT EXISTS "PasswordResetSentAt" timestamp with time zone;
                """);

            // Grandfather: migration oncesi olusan platform kullanicilari dogrulanmis say
            migrationBuilder.Sql("""
                UPDATE "PlatformUsers" SET "IsEmailVerified" = true WHERE "IsEmailVerified" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "PlatformUsers"
                    DROP COLUMN IF EXISTS "EmailVerificationToken",
                    DROP COLUMN IF EXISTS "EmailVerificationSentAt",
                    DROP COLUMN IF EXISTS "PasswordResetToken",
                    DROP COLUMN IF EXISTS "PasswordResetSentAt";
                """);
        }
    }
}
