using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentGatewaySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Oncelikle mukerrer telefon numaralarini temizle (eski kayitlarin Phone ini NULL yap)
            migrationBuilder.Sql("""
                UPDATE "SlnClients" SET "Phone" = NULL
                WHERE "Id" NOT IN (
                    SELECT MIN("Id") FROM "SlnClients"
                    WHERE "Phone" IS NOT NULL
                    GROUP BY "CustomerId", "Phone"
                );
                """);

            // SlnClients index: yoksa olustur, varsa atla (idempotent)
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SlnClients_CustomerId_Phone') THEN
                        CREATE UNIQUE INDEX "IX_SlnClients_CustomerId_Phone" ON "SlnClients" ("CustomerId", "Phone") WHERE "Phone" IS NOT NULL;
                    END IF;
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SlnClients_CustomerId') THEN
                        DROP INDEX "IX_SlnClients_CustomerId";
                    END IF;
                END $$;
                """);

            // SlnClients Phone varchar(20) - zaten uygulanmissa sorun cikarmaz
            migrationBuilder.Sql("""
                ALTER TABLE "SlnClients" ALTER COLUMN "Phone" TYPE character varying(20);
                ALTER TABLE "SlnClients" ALTER COLUMN "Phone2" TYPE character varying(20);
                """);

            migrationBuilder.AddColumn<int>(
                name: "ModuleId",
                table: "PaymentTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformPaymentConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderTypeId = table.Column<int>(type: "integer", nullable: false),
                    EncryptedCredentials = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EncryptedBankInfo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSandbox = table.Column<bool>(type: "boolean", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTestSuccess = table.Column<bool>(type: "boolean", nullable: true),
                    LastTestError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformPaymentConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IntervalMonths = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PeriodPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BillingDay = table.Column<int>(type: "integer", nullable: false),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    PaymentGraceDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSubscriptions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // IX_SlnClients_CustomerId_Phone zaten idempotent SQL ile yukarida olusturuldu

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSubscriptions_CustomerId_StatusId",
                table: "CustomerSubscriptions",
                columns: new[] { "CustomerId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSubscriptions_PlanId",
                table: "CustomerSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPaymentConfigs_ProviderTypeId_IsActive",
                table: "PlatformPaymentConfigs",
                columns: new[] { "ProviderTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPaymentConfigs_Uid",
                table: "PlatformPaymentConfigs",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSubscriptions");

            migrationBuilder.DropTable(
                name: "PlatformPaymentConfigs");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "PaymentTransactions");

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_SlnClients_CustomerId_Phone";
                CREATE INDEX "IX_SlnClients_CustomerId" ON "SlnClients" ("CustomerId");
                ALTER TABLE "SlnClients" ALTER COLUMN "Phone" TYPE text;
                ALTER TABLE "SlnClients" ALTER COLUMN "Phone2" TYPE text;
                """);
        }
    }
}
