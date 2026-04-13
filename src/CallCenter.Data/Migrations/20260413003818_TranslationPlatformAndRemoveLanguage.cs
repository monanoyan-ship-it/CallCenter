using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    public partial class TranslationPlatformAndRemoveLanguage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- TranslationKey.PlatformId ekleme
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='TranslationKeys' AND column_name='PlatformId') THEN
                        ALTER TABLE "TranslationKeys" ADD "PlatformId" integer NOT NULL DEFAULT 5;
                    END IF;
                END $$;

                -- SlnClient.IsActive ekleme
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnClients' AND column_name='IsActive') THEN
                        ALTER TABLE "SlnClients" ADD "IsActive" boolean NOT NULL DEFAULT true;
                    END IF;
                END $$;

                -- Languages FK ve tabloyu kaldir
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Translations_Languages_LanguageCode') THEN
                        ALTER TABLE "Translations" DROP CONSTRAINT "FK_Translations_Languages_LanguageCode";
                    END IF;
                END $$;

                DROP INDEX IF EXISTS "IX_Translations_LanguageCode";
                DROP TABLE IF EXISTS "Languages";

                -- Mevcut key lerin PlatformId sini 5 (CallCenter) yap
                UPDATE "TranslationKeys" SET "PlatformId" = 5 WHERE "PlatformId" = 0;
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PlatformId", table: "TranslationKeys");
            migrationBuilder.DropColumn(name: "IsActive", table: "SlnClients");

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_Languages", x => x.Code); });

            migrationBuilder.InsertData(table: "Languages", columns: new[] { "Code", "IsActive", "IsDefault", "Name" },
                values: new object[,] { { "en", true, false, "English" }, { "tr", true, true, "Türkçe" } });

            migrationBuilder.CreateIndex(name: "IX_Translations_LanguageCode", table: "Translations", column: "LanguageCode");
            migrationBuilder.AddForeignKey(name: "FK_Translations_Languages_LanguageCode", table: "Translations",
                column: "LanguageCode", principalTable: "Languages", principalColumn: "Code", onDelete: ReferentialAction.Cascade);
        }
    }
}
