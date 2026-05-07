using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailAndPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "IsEmailVerified" boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS "EmailVerificationToken" text,
                    ADD COLUMN IF NOT EXISTS "EmailVerificationSentAt" timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS "PasswordResetToken" text,
                    ADD COLUMN IF NOT EXISTS "PasswordResetSentAt" timestamp with time zone;
                """);

            // Grandfather: migration oncesi olusan kullanicilari dogrulanmis say
            migrationBuilder.Sql("""
                UPDATE "Users" SET "IsEmailVerified" = true WHERE "IsEmailVerified" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                    DROP COLUMN IF EXISTS "IsEmailVerified",
                    DROP COLUMN IF EXISTS "EmailVerificationToken",
                    DROP COLUMN IF EXISTS "EmailVerificationSentAt",
                    DROP COLUMN IF EXISTS "PasswordResetToken",
                    DROP COLUMN IF EXISTS "PasswordResetSentAt";
                """);
        }
    }
}
