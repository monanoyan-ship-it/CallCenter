using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══ MEMBERSHIP REDESIGN ═══

            // SlnMembershipUsages: Year/Month → PeriodStart
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipUsages' AND column_name='Year') THEN
                        DROP INDEX IF EXISTS "IX_SlnMembershipUsages_MembershipId_ServiceId_Year_Month";
                        ALTER TABLE "SlnMembershipUsages" DROP COLUMN "Year";
                        ALTER TABLE "SlnMembershipUsages" DROP COLUMN "Month";
                    END IF;
                END $$;
            """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipUsages' AND column_name='PeriodStart') THEN
                        ALTER TABLE "SlnMembershipUsages" ADD "PeriodStart" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipUsages' AND column_name='LastUsedAt') THEN
                        ALTER TABLE "SlnMembershipUsages" ADD "LastUsedAt" timestamp with time zone NOT NULL DEFAULT '0001-01-01';
                    END IF;
                END $$;
            """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SlnMembershipUsages_MembershipId_ServiceId_PeriodStart"
                ON "SlnMembershipUsages" ("MembershipId", "ServiceId", "PeriodStart");
            """);

            // SlnMembershipPlans: FreeSessionsPerMonth → DurationType, MonthlyPrice → Price
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipPlans' AND column_name='FreeSessionsPerMonth') THEN
                        ALTER TABLE "SlnMembershipPlans" RENAME COLUMN "FreeSessionsPerMonth" TO "DurationType";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipPlans' AND column_name='MonthlyPrice') THEN
                        ALTER TABLE "SlnMembershipPlans" DROP COLUMN "MonthlyPrice";
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipPlans' AND column_name='DurationDays') THEN
                        ALTER TABLE "SlnMembershipPlans" ADD "DurationDays" integer NOT NULL DEFAULT 0;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipPlans' AND column_name='Price') THEN
                        ALTER TABLE "SlnMembershipPlans" ADD "Price" numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            """);

            // SlnMembershipPlanServices: FreeCountPerMonth → FreeCount
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnMembershipPlanServices' AND column_name='FreeCountPerMonth') THEN
                        ALTER TABLE "SlnMembershipPlanServices" RENAME COLUMN "FreeCountPerMonth" TO "FreeCount";
                    END IF;
                END $$;
            """);

            // SlnClientMemberships: NextPaymentDate/UsedFreeSessionsThisMonth → CurrentPeriodStart/End/PaidAmount
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnClientMemberships' AND column_name='NextPaymentDate') THEN
                        ALTER TABLE "SlnClientMemberships" DROP COLUMN "NextPaymentDate";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnClientMemberships' AND column_name='UsedFreeSessionsThisMonth') THEN
                        ALTER TABLE "SlnClientMemberships" DROP COLUMN "UsedFreeSessionsThisMonth";
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnClientMemberships' AND column_name='CurrentPeriodStart') THEN
                        ALTER TABLE "SlnClientMemberships" ADD "CurrentPeriodStart" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnClientMemberships' AND column_name='CurrentPeriodEnd') THEN
                        ALTER TABLE "SlnClientMemberships" ADD "CurrentPeriodEnd" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnClientMemberships' AND column_name='PaidAmount') THEN
                        ALTER TABLE "SlnClientMemberships" ADD "PaidAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            """);

            // ═══ MULTI-BRANCH ═══

            // SubscriptionPlan.BranchPrice
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SubscriptionPlans' AND column_name='BranchPrice') THEN
                        ALTER TABLE "SubscriptionPlans" ADD "BranchPrice" numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            """);

            // SlnBranch yeni kolonlar
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='City') THEN
                        ALTER TABLE "SlnBranches" ADD "City" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='District') THEN
                        ALTER TABLE "SlnBranches" ADD "District" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='Email') THEN
                        ALTER TABLE "SlnBranches" ADD "Email" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='WorkingHoursJson') THEN
                        ALTER TABLE "SlnBranches" ADD "WorkingHoursJson" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='Latitude') THEN
                        ALTER TABLE "SlnBranches" ADD "Latitude" double precision;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='Longitude') THEN
                        ALTER TABLE "SlnBranches" ADD "Longitude" double precision;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='PhotoUrl') THEN
                        ALTER TABLE "SlnBranches" ADD "PhotoUrl" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnBranches' AND column_name='IsHeadquarter') THEN
                        ALTER TABLE "SlnBranches" ADD "IsHeadquarter" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
            """);

            // SlnBranch ManagerPersonnelId FK index
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_SlnBranches_ManagerPersonnelId" ON "SlnBranches" ("ManagerPersonnelId");
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_SlnBranches_CustomerPersonnel_ManagerPersonnelId') THEN
                        ALTER TABLE "SlnBranches" ADD CONSTRAINT "FK_SlnBranches_CustomerPersonnel_ManagerPersonnelId"
                            FOREIGN KEY ("ManagerPersonnelId") REFERENCES "CustomerPersonnel" ("Id");
                    END IF;
                END $$;
            """);

            // BranchId kolonları: CustomerPersonnel, SlnAppointments, SlnInvoices, SlnCashRegisters, SlnExpenses, SlnStockMovements
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CustomerPersonnel' AND column_name='BranchId') THEN
                        ALTER TABLE "CustomerPersonnel" ADD "BranchId" integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnAppointments' AND column_name='BranchId') THEN
                        ALTER TABLE "SlnAppointments" ADD "BranchId" integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnInvoices' AND column_name='BranchId') THEN
                        ALTER TABLE "SlnInvoices" ADD "BranchId" integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnCashRegisters' AND column_name='BranchId') THEN
                        ALTER TABLE "SlnCashRegisters" ADD "BranchId" integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnExpenses' AND column_name='BranchId') THEN
                        ALTER TABLE "SlnExpenses" ADD "BranchId" integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnStockMovements' AND column_name='BranchId') THEN
                        ALTER TABLE "SlnStockMovements" ADD "BranchId" integer;
                    END IF;
                END $$;
            """);

            // BranchId FK'ler ve index'ler
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_CustomerPersonnel_BranchId" ON "CustomerPersonnel" ("BranchId");
                CREATE INDEX IF NOT EXISTS "IX_SlnAppointments_BranchId" ON "SlnAppointments" ("BranchId");
                CREATE INDEX IF NOT EXISTS "IX_SlnInvoices_BranchId" ON "SlnInvoices" ("BranchId");
                CREATE INDEX IF NOT EXISTS "IX_SlnCashRegisters_BranchId" ON "SlnCashRegisters" ("BranchId");
                CREATE INDEX IF NOT EXISTS "IX_SlnExpenses_BranchId" ON "SlnExpenses" ("BranchId");
                CREATE INDEX IF NOT EXISTS "IX_SlnStockMovements_BranchId" ON "SlnStockMovements" ("BranchId");

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_CustomerPersonnel_SlnBranches_BranchId') THEN
                        ALTER TABLE "CustomerPersonnel" ADD CONSTRAINT "FK_CustomerPersonnel_SlnBranches_BranchId"
                            FOREIGN KEY ("BranchId") REFERENCES "SlnBranches" ("Id") ON DELETE SET NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_SlnAppointments_SlnBranches_BranchId') THEN
                        ALTER TABLE "SlnAppointments" ADD CONSTRAINT "FK_SlnAppointments_SlnBranches_BranchId"
                            FOREIGN KEY ("BranchId") REFERENCES "SlnBranches" ("Id");
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_SlnInvoices_SlnBranches_BranchId') THEN
                        ALTER TABLE "SlnInvoices" ADD CONSTRAINT "FK_SlnInvoices_SlnBranches_BranchId"
                            FOREIGN KEY ("BranchId") REFERENCES "SlnBranches" ("Id");
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_SlnCashRegisters_SlnBranches_BranchId') THEN
                        ALTER TABLE "SlnCashRegisters" ADD CONSTRAINT "FK_SlnCashRegisters_SlnBranches_BranchId"
                            FOREIGN KEY ("BranchId") REFERENCES "SlnBranches" ("Id");
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_SlnExpenses_SlnBranches_BranchId') THEN
                        ALTER TABLE "SlnExpenses" ADD CONSTRAINT "FK_SlnExpenses_SlnBranches_BranchId"
                            FOREIGN KEY ("BranchId") REFERENCES "SlnBranches" ("Id");
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_SlnStockMovements_SlnBranches_BranchId') THEN
                        ALTER TABLE "SlnStockMovements" ADD CONSTRAINT "FK_SlnStockMovements_SlnBranches_BranchId"
                            FOREIGN KEY ("BranchId") REFERENCES "SlnBranches" ("Id");
                    END IF;
                END $$;
            """);

            // SlnRecipes.PhotoUrl
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SlnRecipes' AND column_name='PhotoUrl') THEN
                        ALTER TABLE "SlnRecipes" ADD "PhotoUrl" text;
                    END IF;
                END $$;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_CustomerPersonnel_SlnBranches_BranchId", table: "CustomerPersonnel");
            migrationBuilder.DropForeignKey(name: "FK_SlnAppointments_SlnBranches_BranchId", table: "SlnAppointments");
            migrationBuilder.DropForeignKey(name: "FK_SlnBranches_CustomerPersonnel_ManagerPersonnelId", table: "SlnBranches");
            migrationBuilder.DropForeignKey(name: "FK_SlnCashRegisters_SlnBranches_BranchId", table: "SlnCashRegisters");
            migrationBuilder.DropForeignKey(name: "FK_SlnExpenses_SlnBranches_BranchId", table: "SlnExpenses");
            migrationBuilder.DropForeignKey(name: "FK_SlnInvoices_SlnBranches_BranchId", table: "SlnInvoices");
            migrationBuilder.DropForeignKey(name: "FK_SlnStockMovements_SlnBranches_BranchId", table: "SlnStockMovements");

            migrationBuilder.DropIndex(name: "IX_SlnStockMovements_BranchId", table: "SlnStockMovements");
            migrationBuilder.DropIndex(name: "IX_SlnMembershipUsages_MembershipId_ServiceId_PeriodStart", table: "SlnMembershipUsages");
            migrationBuilder.DropIndex(name: "IX_SlnInvoices_BranchId", table: "SlnInvoices");
            migrationBuilder.DropIndex(name: "IX_SlnExpenses_BranchId", table: "SlnExpenses");
            migrationBuilder.DropIndex(name: "IX_SlnCashRegisters_BranchId", table: "SlnCashRegisters");
            migrationBuilder.DropIndex(name: "IX_SlnBranches_ManagerPersonnelId", table: "SlnBranches");
            migrationBuilder.DropIndex(name: "IX_SlnAppointments_BranchId", table: "SlnAppointments");
            migrationBuilder.DropIndex(name: "IX_CustomerPersonnel_BranchId", table: "CustomerPersonnel");

            migrationBuilder.DropColumn(name: "BranchPrice", table: "SubscriptionPlans");
            migrationBuilder.DropColumn(name: "BranchId", table: "SlnStockMovements");
            migrationBuilder.DropColumn(name: "PhotoUrl", table: "SlnRecipes");
            migrationBuilder.DropColumn(name: "LastUsedAt", table: "SlnMembershipUsages");
            migrationBuilder.DropColumn(name: "PeriodStart", table: "SlnMembershipUsages");
            migrationBuilder.DropColumn(name: "DurationDays", table: "SlnMembershipPlans");
            migrationBuilder.DropColumn(name: "Price", table: "SlnMembershipPlans");
            migrationBuilder.DropColumn(name: "BranchId", table: "SlnInvoices");
            migrationBuilder.DropColumn(name: "BranchId", table: "SlnExpenses");
            migrationBuilder.DropColumn(name: "CurrentPeriodEnd", table: "SlnClientMemberships");
            migrationBuilder.DropColumn(name: "CurrentPeriodStart", table: "SlnClientMemberships");
            migrationBuilder.DropColumn(name: "PaidAmount", table: "SlnClientMemberships");
            migrationBuilder.DropColumn(name: "BranchId", table: "SlnCashRegisters");
            migrationBuilder.DropColumn(name: "City", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "District", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "Email", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "IsHeadquarter", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "Latitude", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "Longitude", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "PhotoUrl", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "WorkingHoursJson", table: "SlnBranches");
            migrationBuilder.DropColumn(name: "BranchId", table: "SlnAppointments");
            migrationBuilder.DropColumn(name: "BranchId", table: "CustomerPersonnel");

            migrationBuilder.RenameColumn(name: "FreeCount", table: "SlnMembershipPlanServices", newName: "FreeCountPerMonth");
            migrationBuilder.RenameColumn(name: "DurationType", table: "SlnMembershipPlans", newName: "FreeSessionsPerMonth");

            migrationBuilder.AddColumn<int>(name: "Month", table: "SlnMembershipUsages", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "Year", table: "SlnMembershipUsages", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<decimal>(name: "MonthlyPrice", table: "SlnMembershipPlans", type: "numeric", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<DateTime>(name: "NextPaymentDate", table: "SlnClientMemberships", type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            migrationBuilder.AddColumn<int>(name: "UsedFreeSessionsThisMonth", table: "SlnClientMemberships", type: "integer", nullable: false, defaultValue: 0);

            migrationBuilder.CreateIndex(name: "IX_SlnMembershipUsages_MembershipId_ServiceId_Year_Month", table: "SlnMembershipUsages", columns: new[] { "MembershipId", "ServiceId", "Year", "Month" }, unique: true);
        }
    }
}
