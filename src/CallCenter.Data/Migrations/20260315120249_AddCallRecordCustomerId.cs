using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCallRecordCustomerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_CustomerId",
                table: "CallRecords",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallRecords_Customers_CustomerId",
                table: "CallRecords",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallRecords_Customers_CustomerId",
                table: "CallRecords");

            migrationBuilder.DropIndex(
                name: "IX_CallRecords_CustomerId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "CallRecords");
        }
    }
}
