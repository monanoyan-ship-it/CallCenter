using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonLoyaltyProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnLoyaltyPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    RewardServiceId = table.Column<int>(type: "integer", nullable: false),
                    RequiredVisits = table.Column<int>(type: "integer", nullable: false),
                    RewardValidDays = table.Column<int>(type: "integer", nullable: true),
                    MaxRewardsPerClient = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnLoyaltyPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPrograms_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPrograms_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPrograms_SlnServices_RewardServiceId",
                        column: x => x.RewardServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyPrograms_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlnClientLoyaltyProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    VisitCount = table.Column<int>(type: "integer", nullable: false),
                    RewardsEarned = table.Column<int>(type: "integer", nullable: false),
                    RewardsUsed = table.Column<int>(type: "integer", nullable: false),
                    LastVisitAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnClientLoyaltyProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnClientLoyaltyProgresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnClientLoyaltyProgresses_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnClientLoyaltyProgresses_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnClientLoyaltyProgresses_SlnLoyaltyPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "SlnLoyaltyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnLoyaltyProgramRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgressId = table.Column<int>(type: "integer", nullable: false),
                    RewardServiceId = table.Column<int>(type: "integer", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EarnedFromInvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedInvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnLoyaltyProgramRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyProgramRewards_SlnClientLoyaltyProgresses_Progres~",
                        column: x => x.ProgressId,
                        principalTable: "SlnClientLoyaltyProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyProgramRewards_SlnServices_RewardServiceId",
                        column: x => x.RewardServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLoyaltyProgresses_BranchId",
                table: "SlnClientLoyaltyProgresses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLoyaltyProgresses_CustomerId_BranchId",
                table: "SlnClientLoyaltyProgresses",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLoyaltyProgresses_CustomerId_SlnClientId_ProgramId",
                table: "SlnClientLoyaltyProgresses",
                columns: new[] { "CustomerId", "SlnClientId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLoyaltyProgresses_ProgramId",
                table: "SlnClientLoyaltyProgresses",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLoyaltyProgresses_SlnClientId",
                table: "SlnClientLoyaltyProgresses",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyProgramRewards_ProgressId",
                table: "SlnLoyaltyProgramRewards",
                column: "ProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyProgramRewards_RewardServiceId",
                table: "SlnLoyaltyProgramRewards",
                column: "RewardServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyProgramRewards_UsedInvoiceItemId",
                table: "SlnLoyaltyProgramRewards",
                column: "UsedInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPrograms_BranchId",
                table: "SlnLoyaltyPrograms",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPrograms_CustomerId_BranchId",
                table: "SlnLoyaltyPrograms",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPrograms_CustomerId_ServiceId",
                table: "SlnLoyaltyPrograms",
                columns: new[] { "CustomerId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPrograms_RewardServiceId",
                table: "SlnLoyaltyPrograms",
                column: "RewardServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyPrograms_ServiceId",
                table: "SlnLoyaltyPrograms",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnLoyaltyProgramRewards");

            migrationBuilder.DropTable(
                name: "SlnClientLoyaltyProgresses");

            migrationBuilder.DropTable(
                name: "SlnLoyaltyPrograms");
        }
    }
}
