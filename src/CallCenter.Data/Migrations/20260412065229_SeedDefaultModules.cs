using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    public partial class SeedDefaultModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut salon musterilerine default modulleri ekle (yoksa)
            // Default module IDs: 201,202,203,204,206,209,214,215,220
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerPortalModules"" (""CustomerId"", ""ModuleId"", ""IsActive"", ""ActivatedAt"")
                SELECT c.""Id"", m.""ModuleId"", true, NOW()
                FROM ""Customers"" c
                CROSS JOIN (VALUES (201),(202),(203),(204),(206),(209),(214),(215),(220)) AS m(""ModuleId"")
                LEFT JOIN ""CustomerPortalModules"" cpm 
                    ON cpm.""CustomerId"" = c.""Id"" AND cpm.""ModuleId"" = m.""ModuleId""
                WHERE cpm.""Id"" IS NULL
                  AND EXISTS (SELECT 1 FROM ""CustomerProducts"" cp WHERE cp.""CustomerId"" = c.""Id"" AND cp.""ProductTypeId"" = 2);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down: nothing (data migration)
        }
    }
}
