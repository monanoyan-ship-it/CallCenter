using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCashRegisterBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BranchId null olan kasalari merkez subeye bagla
            migrationBuilder.Sql(@"
                UPDATE ""SlnCashRegisters"" cr
                SET ""BranchId"" = b.""Id""
                FROM ""SlnBranches"" b
                WHERE cr.""BranchId"" IS NULL
                  AND b.""CustomerId"" = cr.""CustomerId""
                  AND b.""IsHeadquarter"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
