using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCallRecordCallbackFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CallbackAssignedToId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallbackNote",
                table: "CallRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CallbackResultCallId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CallbackStatusId",
                table: "CallRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_CallbackAssignedToId_CallbackStatusId",
                table: "CallRecords",
                columns: new[] { "CallbackAssignedToId", "CallbackStatusId" },
                filter: "\"CallbackStatusId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_CallbackResultCallId",
                table: "CallRecords",
                column: "CallbackResultCallId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallRecords_CallRecords_CallbackResultCallId",
                table: "CallRecords",
                column: "CallbackResultCallId",
                principalTable: "CallRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CallRecords_Users_CallbackAssignedToId",
                table: "CallRecords",
                column: "CallbackAssignedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallRecords_CallRecords_CallbackResultCallId",
                table: "CallRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CallRecords_Users_CallbackAssignedToId",
                table: "CallRecords");

            migrationBuilder.DropIndex(
                name: "IX_CallRecords_CallbackAssignedToId_CallbackStatusId",
                table: "CallRecords");

            migrationBuilder.DropIndex(
                name: "IX_CallRecords_CallbackResultCallId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CallbackAssignedToId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CallbackNote",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CallbackResultCallId",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "CallbackStatusId",
                table: "CallRecords");
        }
    }
}
