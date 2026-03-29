using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFazDEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnBeforeAfterPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    BeforePhotoUrl = table.Column<string>(type: "text", nullable: true),
                    AfterPhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnBeforeAfterPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnBeforeAfterPhotos_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnBeforeAfterPhotos_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnBeforeAfterPhotos_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnBeforeAfterPhotos_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlnConsentForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    RequireSignature = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnConsentForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnConsentForms_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnPersonnelServicePrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPersonnelServicePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelServicePrices_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelServicePrices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelServicePrices_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnRevenueShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    ModelTypeId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelSharePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumGuarantee = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnRevenueShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnRevenueShares_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnRevenueShares_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnWinbackRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    InactiveDays = table.Column<int>(type: "integer", nullable: false),
                    ChannelId = table.Column<int>(type: "integer", nullable: false),
                    MessageTemplate = table.Column<string>(type: "text", nullable: false),
                    DiscountPercent = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnWinbackRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnWinbackRules_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnClientConsents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    SignatureData = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnClientConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnClientConsents_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnClientConsents_SlnConsentForms_FormId",
                        column: x => x.FormId,
                        principalTable: "SlnConsentForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnBeforeAfterPhotos_CustomerId",
                table: "SlnBeforeAfterPhotos",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnBeforeAfterPhotos_PersonnelId",
                table: "SlnBeforeAfterPhotos",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnBeforeAfterPhotos_ServiceId",
                table: "SlnBeforeAfterPhotos",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnBeforeAfterPhotos_SlnClientId",
                table: "SlnBeforeAfterPhotos",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientConsents_FormId",
                table: "SlnClientConsents",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientConsents_SlnClientId",
                table: "SlnClientConsents",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnConsentForms_CustomerId",
                table: "SlnConsentForms",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelServicePrices_CustomerId",
                table: "SlnPersonnelServicePrices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelServicePrices_PersonnelId",
                table: "SlnPersonnelServicePrices",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelServicePrices_ServiceId",
                table: "SlnPersonnelServicePrices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnRevenueShares_CustomerId",
                table: "SlnRevenueShares",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnRevenueShares_PersonnelId",
                table: "SlnRevenueShares",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnWinbackRules_CustomerId",
                table: "SlnWinbackRules",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnBeforeAfterPhotos");

            migrationBuilder.DropTable(
                name: "SlnClientConsents");

            migrationBuilder.DropTable(
                name: "SlnPersonnelServicePrices");

            migrationBuilder.DropTable(
                name: "SlnRevenueShares");

            migrationBuilder.DropTable(
                name: "SlnWinbackRules");

            migrationBuilder.DropTable(
                name: "SlnConsentForms");
        }
    }
}
