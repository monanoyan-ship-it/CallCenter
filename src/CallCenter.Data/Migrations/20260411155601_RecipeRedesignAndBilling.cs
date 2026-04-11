using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class RecipeRedesignAndBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut recipe item'lari temizle (ServiceId -> ProductId donusumu icin)
            migrationBuilder.Sql("""DELETE FROM "SlnRecipeItems";""");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnRecipeItems_SlnServices_ServiceId",
                table: "SlnRecipeItems");

            migrationBuilder.DropColumn(
                name: "TotalDurationMinutes",
                table: "SlnRecipes");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "SlnRecipes");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "SlnRecipeItems",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_SlnRecipeItems_ServiceId",
                table: "SlnRecipeItems",
                newName: "IX_SlnRecipeItems_ProductId");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "SlnRecipes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "SlnRecipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "SlnRecipeItems",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "SlnRecipeItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "SlnRecipeItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "SlnRecipeItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialCost",
                table: "SlnFormulas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RecipeId",
                table: "SlnFormulas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "SlnFormulas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCompanyName",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingDistrict",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingFullName",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingPostalCode",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingTaxNumber",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingTaxOffice",
                table: "PlatformUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingType",
                table: "PlatformUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SlnRecipes_ServiceId",
                table: "SlnRecipes",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnFormulas_RecipeId",
                table: "SlnFormulas",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnFormulas_ServiceId",
                table: "SlnFormulas",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnFormulas_SlnRecipes_RecipeId",
                table: "SlnFormulas",
                column: "RecipeId",
                principalTable: "SlnRecipes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnFormulas_SlnServices_ServiceId",
                table: "SlnFormulas",
                column: "ServiceId",
                principalTable: "SlnServices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnRecipeItems_SlnProducts_ProductId",
                table: "SlnRecipeItems",
                column: "ProductId",
                principalTable: "SlnProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnRecipes_SlnServices_ServiceId",
                table: "SlnRecipes",
                column: "ServiceId",
                principalTable: "SlnServices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnFormulas_SlnRecipes_RecipeId",
                table: "SlnFormulas");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnFormulas_SlnServices_ServiceId",
                table: "SlnFormulas");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnRecipeItems_SlnProducts_ProductId",
                table: "SlnRecipeItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnRecipes_SlnServices_ServiceId",
                table: "SlnRecipes");

            migrationBuilder.DropIndex(
                name: "IX_SlnRecipes_ServiceId",
                table: "SlnRecipes");

            migrationBuilder.DropIndex(
                name: "IX_SlnFormulas_RecipeId",
                table: "SlnFormulas");

            migrationBuilder.DropIndex(
                name: "IX_SlnFormulas_ServiceId",
                table: "SlnFormulas");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "SlnRecipes");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "SlnRecipes");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "SlnRecipeItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "SlnRecipeItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "SlnRecipeItems");

            migrationBuilder.DropColumn(
                name: "MaterialCost",
                table: "SlnFormulas");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "SlnFormulas");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "SlnFormulas");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingCity",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingCompanyName",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingDistrict",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingFullName",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingPostalCode",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingTaxNumber",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingTaxOffice",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "BillingType",
                table: "PlatformUsers");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "SlnRecipeItems",
                newName: "ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_SlnRecipeItems_ProductId",
                table: "SlnRecipeItems",
                newName: "IX_SlnRecipeItems_ServiceId");

            migrationBuilder.AddColumn<int>(
                name: "TotalDurationMinutes",
                table: "SlnRecipes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "SlnRecipes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "SlnRecipeItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)",
                oldPrecision: 10,
                oldScale: 3);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnRecipeItems_SlnServices_ServiceId",
                table: "SlnRecipeItems",
                column: "ServiceId",
                principalTable: "SlnServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
