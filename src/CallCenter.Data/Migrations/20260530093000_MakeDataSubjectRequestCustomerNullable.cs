using CallCenter.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(AppDbContext))]
    [Migration("20260530093000_MakeDataSubjectRequestCustomerNullable")]
    public partial class MakeDataSubjectRequestCustomerNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataSubjectRequests_Customers_CustomerId",
                table: "DataSubjectRequests");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "DataSubjectRequests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_DataSubjectRequests_Customers_CustomerId",
                table: "DataSubjectRequests",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataSubjectRequests_Customers_CustomerId",
                table: "DataSubjectRequests");

            migrationBuilder.Sql("""DELETE FROM "DataSubjectRequests" WHERE "CustomerId" IS NULL""");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "DataSubjectRequests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DataSubjectRequests_Customers_CustomerId",
                table: "DataSubjectRequests",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
