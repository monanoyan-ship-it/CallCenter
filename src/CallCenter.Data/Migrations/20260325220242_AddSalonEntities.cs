using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnAdvances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    AdvanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnAdvances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnAdvances_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnBankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    AccountNo = table.Column<string>(type: "text", nullable: true),
                    IBAN = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnBankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnBankAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnCashRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnCashRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnCashRegisters_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Phone2 = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    GenderId = table.Column<int>(type: "integer", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MarriageDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Occupation = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    HairColor = table.Column<string>(type: "text", nullable: true),
                    WhiteRatioPercent = table.Column<int>(type: "integer", nullable: true),
                    SkinType = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnClients_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnExpenseCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnExpenseCategories_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnPayrolls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "numeric", nullable: false),
                    ServiceCommission = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductCommission = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAdvance = table.Column<decimal>(type: "numeric", nullable: false),
                    Deductions = table.Column<decimal>(type: "numeric", nullable: false),
                    NetPay = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPayrolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPayrolls_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnProductBrands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnProductBrands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnProductBrands_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnProductCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnProductCategories_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnServiceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnServiceCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnServiceCategories_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnSuppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    TaxNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnSuppliers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnPosDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    BankAccountId = table.Column<int>(type: "integer", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPosDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPosDevices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPosDevices_SlnBankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "SlnBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnClientPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnClientPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnClientPhotos_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnFormulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    FormulaText = table.Column<string>(type: "text", nullable: false),
                    ColorCode = table.Column<string>(type: "text", nullable: true),
                    OxidantRatio = table.Column<string>(type: "text", nullable: true),
                    ApplicationNotes = table.Column<string>(type: "text", nullable: true),
                    AppliedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnFormulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnFormulas_CustomerPersonnel_AppliedByPersonnelId",
                        column: x => x.AppliedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnFormulas_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnExpenses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnExpenses_SlnExpenseCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SlnExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    BrandId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    StockQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    MinStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnProducts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnProducts_SlnProductBrands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "SlnProductBrands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnProducts_SlnProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SlnProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnServices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServices_SlnServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SlnServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnSupplierTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnSupplierTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnSupplierTransactions_SlnSuppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "SlnSuppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceNo = table.Column<string>(type: "text", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: false),
                    PosDeviceId = table.Column<int>(type: "integer", nullable: true),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnInvoices_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnInvoices_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnInvoices_SlnPosDevices_PosDeviceId",
                        column: x => x.PosDeviceId,
                        principalTable: "SlnPosDevices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlnStockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    MovementTypeId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnStockMovements_SlnProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SlnProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnStockMovements_SlnSuppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "SlnSuppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlnAppointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnAppointments_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnAppointments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnAppointments_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnAppointments_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnPersonnelCommissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    IsPercentage = table.Column<bool>(type: "boolean", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPersonnelCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelCommissions_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelCommissions_SlnProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SlnProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnPersonnelCommissions_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlnPersonnelSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPersonnelSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelSkills_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelSkills_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnCashTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegisterId = table.Column<int>(type: "integer", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: false),
                    RelatedInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnCashTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnCashTransactions_SlnCashRegisters_RegisterId",
                        column: x => x.RegisterId,
                        principalTable: "SlnCashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnCashTransactions_SlnInvoices_RelatedInvoiceId",
                        column: x => x.RelatedInvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlnInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnInvoiceItems_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnInvoiceItems_SlnInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnInvoiceItems_SlnProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SlnProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnInvoiceItems_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnAdvances_PersonnelId",
                table: "SlnAdvances",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnAppointments_CustomerId",
                table: "SlnAppointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnAppointments_PersonnelId",
                table: "SlnAppointments",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnAppointments_ServiceId",
                table: "SlnAppointments",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnAppointments_SlnClientId",
                table: "SlnAppointments",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnBankAccounts_CustomerId",
                table: "SlnBankAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnCashRegisters_CustomerId",
                table: "SlnCashRegisters",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnCashTransactions_RegisterId",
                table: "SlnCashTransactions",
                column: "RegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnCashTransactions_RelatedInvoiceId",
                table: "SlnCashTransactions",
                column: "RelatedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientPhotos_SlnClientId",
                table: "SlnClientPhotos",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClients_CustomerId",
                table: "SlnClients",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnExpenseCategories_CustomerId",
                table: "SlnExpenseCategories",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnExpenses_CategoryId",
                table: "SlnExpenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnExpenses_CustomerId",
                table: "SlnExpenses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnFormulas_AppliedByPersonnelId",
                table: "SlnFormulas",
                column: "AppliedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnFormulas_SlnClientId",
                table: "SlnFormulas",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoiceItems_InvoiceId",
                table: "SlnInvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoiceItems_PersonnelId",
                table: "SlnInvoiceItems",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoiceItems_ProductId",
                table: "SlnInvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoiceItems_ServiceId",
                table: "SlnInvoiceItems",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoices_CustomerId",
                table: "SlnInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoices_PersonnelId",
                table: "SlnInvoices",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoices_PosDeviceId",
                table: "SlnInvoices",
                column: "PosDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnInvoices_SlnClientId",
                table: "SlnInvoices",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPayrolls_PersonnelId",
                table: "SlnPayrolls",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelCommissions_PersonnelId",
                table: "SlnPersonnelCommissions",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelCommissions_ProductId",
                table: "SlnPersonnelCommissions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelCommissions_ServiceId",
                table: "SlnPersonnelCommissions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelSkills_PersonnelId",
                table: "SlnPersonnelSkills",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelSkills_ServiceId",
                table: "SlnPersonnelSkills",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPosDevices_BankAccountId",
                table: "SlnPosDevices",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPosDevices_CustomerId",
                table: "SlnPosDevices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnProductBrands_CustomerId",
                table: "SlnProductBrands",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnProductCategories_CustomerId",
                table: "SlnProductCategories",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnProducts_BrandId",
                table: "SlnProducts",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnProducts_CategoryId",
                table: "SlnProducts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnProducts_CustomerId",
                table: "SlnProducts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceCategories_CustomerId",
                table: "SlnServiceCategories",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServices_CategoryId",
                table: "SlnServices",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServices_CustomerId",
                table: "SlnServices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnStockMovements_ProductId",
                table: "SlnStockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnStockMovements_SupplierId",
                table: "SlnStockMovements",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnSuppliers_CustomerId",
                table: "SlnSuppliers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnSupplierTransactions_SupplierId",
                table: "SlnSupplierTransactions",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnAdvances");

            migrationBuilder.DropTable(
                name: "SlnAppointments");

            migrationBuilder.DropTable(
                name: "SlnCashTransactions");

            migrationBuilder.DropTable(
                name: "SlnClientPhotos");

            migrationBuilder.DropTable(
                name: "SlnExpenses");

            migrationBuilder.DropTable(
                name: "SlnFormulas");

            migrationBuilder.DropTable(
                name: "SlnInvoiceItems");

            migrationBuilder.DropTable(
                name: "SlnPayrolls");

            migrationBuilder.DropTable(
                name: "SlnPersonnelCommissions");

            migrationBuilder.DropTable(
                name: "SlnPersonnelSkills");

            migrationBuilder.DropTable(
                name: "SlnStockMovements");

            migrationBuilder.DropTable(
                name: "SlnSupplierTransactions");

            migrationBuilder.DropTable(
                name: "SlnCashRegisters");

            migrationBuilder.DropTable(
                name: "SlnExpenseCategories");

            migrationBuilder.DropTable(
                name: "SlnInvoices");

            migrationBuilder.DropTable(
                name: "SlnServices");

            migrationBuilder.DropTable(
                name: "SlnProducts");

            migrationBuilder.DropTable(
                name: "SlnSuppliers");

            migrationBuilder.DropTable(
                name: "SlnClients");

            migrationBuilder.DropTable(
                name: "SlnPosDevices");

            migrationBuilder.DropTable(
                name: "SlnServiceCategories");

            migrationBuilder.DropTable(
                name: "SlnProductBrands");

            migrationBuilder.DropTable(
                name: "SlnProductCategories");

            migrationBuilder.DropTable(
                name: "SlnBankAccounts");
        }
    }
}
