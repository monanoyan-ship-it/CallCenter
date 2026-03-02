using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIvrAutoAttendant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsWorkday = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessHours_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GreetingMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AudioFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AudioFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GreetingMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GreetingMessages_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoldMusics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    QueueId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AudioFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AudioFileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldMusics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoldMusics_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoldMusics_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GreetingMessageId = table.Column<int>(type: "integer", nullable: true),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Holidays_GreetingMessages_GreetingMessageId",
                        column: x => x.GreetingMessageId,
                        principalTable: "GreetingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IvrMenus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GreetingMessageId = table.Column<int>(type: "integer", nullable: true),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IvrMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IvrMenus_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IvrMenus_GreetingMessages_GreetingMessageId",
                        column: x => x.GreetingMessageId,
                        principalTable: "GreetingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IvrMenuOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IvrMenuId = table.Column<int>(type: "integer", nullable: false),
                    Digit = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetQueueId = table.Column<int>(type: "integer", nullable: true),
                    TargetExtension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetIvrMenuId = table.Column<int>(type: "integer", nullable: true),
                    TargetGreetingMessageId = table.Column<int>(type: "integer", nullable: true),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IvrMenuOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IvrMenuOptions_GreetingMessages_TargetGreetingMessageId",
                        column: x => x.TargetGreetingMessageId,
                        principalTable: "GreetingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IvrMenuOptions_IvrMenus_IvrMenuId",
                        column: x => x.IvrMenuId,
                        principalTable: "IvrMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IvrMenuOptions_IvrMenus_TargetIvrMenuId",
                        column: x => x.TargetIvrMenuId,
                        principalTable: "IvrMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IvrMenuOptions_Queues_TargetQueueId",
                        column: x => x.TargetQueueId,
                        principalTable: "Queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_CustomerId_DayOfWeek",
                table: "BusinessHours",
                columns: new[] { "CustomerId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GreetingMessages_CustomerId_Type",
                table: "GreetingMessages",
                columns: new[] { "CustomerId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_GreetingMessages_Uid",
                table: "GreetingMessages",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoldMusics_CustomerId_QueueId",
                table: "HoldMusics",
                columns: new[] { "CustomerId", "QueueId" });

            migrationBuilder.CreateIndex(
                name: "IX_HoldMusics_QueueId",
                table: "HoldMusics",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldMusics_Uid",
                table: "HoldMusics",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_CustomerId_Date",
                table: "Holidays",
                columns: new[] { "CustomerId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_GreetingMessageId",
                table: "Holidays",
                column: "GreetingMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_IvrMenuOptions_IvrMenuId_Digit",
                table: "IvrMenuOptions",
                columns: new[] { "IvrMenuId", "Digit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IvrMenuOptions_TargetGreetingMessageId",
                table: "IvrMenuOptions",
                column: "TargetGreetingMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_IvrMenuOptions_TargetIvrMenuId",
                table: "IvrMenuOptions",
                column: "TargetIvrMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_IvrMenuOptions_TargetQueueId",
                table: "IvrMenuOptions",
                column: "TargetQueueId");

            migrationBuilder.CreateIndex(
                name: "IX_IvrMenus_CustomerId_Name",
                table: "IvrMenus",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IvrMenus_GreetingMessageId",
                table: "IvrMenus",
                column: "GreetingMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_IvrMenus_Uid",
                table: "IvrMenus",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessHours");

            migrationBuilder.DropTable(
                name: "HoldMusics");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "IvrMenuOptions");

            migrationBuilder.DropTable(
                name: "IvrMenus");

            migrationBuilder.DropTable(
                name: "GreetingMessages");
        }
    }
}
