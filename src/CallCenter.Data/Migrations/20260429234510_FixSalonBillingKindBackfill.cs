using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSalonBillingKindBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Modul satiri varsa kesinlikle salon platform tahakkuku (onceki migration atlanmissa)
            migrationBuilder.Sql(@"
UPDATE ""CustomerBillingPeriods"" p
SET ""BillingKindId"" = 2
WHERE p.""BillingKindId"" = 1
AND EXISTS (
  SELECT 1 FROM ""CustomerBillingPeriodModuleLines"" l
  WHERE l.""CustomerBillingPeriodId"" = p.""Id"");
");

            // 2) Aktif aboneligi olan ve AKTIF Call Center urunu OLMAYAN musteriler: kalan 1'ler salon tahakkuku sayilir
            //    (modul satiri olmayan donemler dahil). Hibrit musterilerde CC urunu oldugu icin bu adim atlanir.
            migrationBuilder.Sql(@"
UPDATE ""CustomerBillingPeriods"" p
SET ""BillingKindId"" = 2
WHERE p.""BillingKindId"" = 1
AND EXISTS (
  SELECT 1 FROM ""CustomerSubscriptions"" s
  WHERE s.""CustomerId"" = p.""CustomerId"" AND s.""StatusId"" = 1
)
AND NOT EXISTS (
  SELECT 1 FROM ""CustomerProducts"" cp
  WHERE cp.""CustomerId"" = p.""CustomerId""
    AND cp.""ProductTypeId"" = 1
    AND cp.""IsActive"" = true
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Veri duzeltmesi geri alinmaz
        }
    }
}
