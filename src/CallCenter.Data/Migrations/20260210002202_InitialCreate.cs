using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranslationKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    Extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPortalModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ModuleId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPortalModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPortalModules_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerUserTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUserTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerUserTypes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Queues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MaxWaitTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Queues_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SipAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Server = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Transport = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    UseSrtp = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SipAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SipAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TranslationKeyId = table.Column<int>(type: "integer", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(5)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Translations_Languages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalTable: "Languages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Translations_TranslationKeys_TranslationKeyId",
                        column: x => x.TranslationKeyId,
                        principalTable: "TranslationKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPersonnel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    UserTypeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPersonnel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnel_CustomerUserTypes_UserTypeId",
                        column: x => x.UserTypeId,
                        principalTable: "CustomerUserTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnel_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnel_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerUserTypePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserTypeId = table.Column<int>(type: "integer", nullable: false),
                    PermissionTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUserTypePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerUserTypePermissions_CustomerUserTypes_UserTypeId",
                        column: x => x.UserTypeId,
                        principalTable: "CustomerUserTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CallerNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CalleeNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DirectionId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordingUrl = table.Column<string>(type: "text", nullable: true),
                    AgentId = table.Column<int>(type: "integer", nullable: true),
                    QueueId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallRecords_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CallRecords_Users_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QueueAgents",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "integer", nullable: false),
                    AgentId = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueAgents", x => new { x.QueueId, x.AgentId });
                    table.ForeignKey(
                        name: "FK_QueueAgents_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueAgents_Users_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPersonnelPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    PermissionTypeId = table.Column<int>(type: "integer", nullable: false),
                    ScopeId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPersonnelPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnelPermissions_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnelPermissions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Code", "IsActive", "IsDefault", "Name" },
                values: new object[,]
                {
                    { "en", true, false, "English" },
                    { "tr", true, true, "Türkçe" }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Description", "Group", "IsSystem", "Key", "Value", "ValueType" },
                values: new object[,]
                {
                    { 1, "Uygulama adi", "general", true, "app.name", "Call Center", "string" },
                    { 2, "Varsayilan dil", "general", true, "app.language", "tr", "string" },
                    { 3, "Zaman dilimi", "general", true, "app.timezone", "Europe/Istanbul", "string" },
                    { 4, "Tarih formati", "general", true, "app.date_format", "dd.MM.yyyy", "string" },
                    { 10, "Maks hatali giris denemesi", "security", true, "security.max_login_attempts", "5", "int" },
                    { 11, "Hesap kilitleme suresi (dk)", "security", true, "security.lockout_minutes", "15", "int" },
                    { 12, "JWT token suresi (dk)", "security", true, "security.token_expire_minutes", "480", "int" },
                    { 13, "Minimum sifre uzunlugu", "security", true, "security.password_min_length", "6", "int" },
                    { 20, "Varsayilan SIP transport", "sip", true, "sip.default_transport", "UDP", "string" },
                    { 21, "SIP kayit suresi (sn)", "sip", true, "sip.registration_timeout", "3600", "int" },
                    { 22, "Keep-alive araligi (sn)", "sip", true, "sip.keep_alive_interval", "30", "int" },
                    { 30, "Bildirim sesi", "notification", false, "notification.sound_enabled", "true", "bool" },
                    { 31, "Masaustu bildirimi", "notification", false, "notification.desktop_enabled", "true", "bool" },
                    { 32, "Zil calma suresi (sn)", "notification", false, "notification.ring_duration", "30", "int" }
                });

            migrationBuilder.InsertData(
                table: "TranslationKeys",
                columns: new[] { "Id", "Description", "Key", "Module" },
                values: new object[,]
                {
                    { 1, "Login butonu", "auth.login", "auth" },
                    { 2, "Çıkış butonu", "auth.logout", "auth" },
                    { 3, "Kullanıcı adı", "auth.username", "auth" },
                    { 4, "Şifre", "auth.password", "auth" },
                    { 5, "Hatalı giriş", "auth.login_failed", "auth" },
                    { 10, "Müsait durumu", "agent.status.available", "agent" },
                    { 11, "Meşgul durumu", "agent.status.busy", "agent" },
                    { 12, "Mola durumu", "agent.status.on_break", "agent" },
                    { 13, "Çağrıda durumu", "agent.status.in_call", "agent" },
                    { 14, "Çevrimdışı durumu", "agent.status.offline", "agent" },
                    { 15, "Çağrı sonrası iş", "agent.status.acw", "agent" },
                    { 20, "Kaydet butonu", "common.save", "common" },
                    { 21, "İptal butonu", "common.cancel", "common" },
                    { 22, "Sil butonu", "common.delete", "common" },
                    { 23, "Düzenle butonu", "common.edit", "common" },
                    { 24, "Arama", "common.search", "common" },
                    { 25, "Yükleniyor", "common.loading", "common" },
                    { 26, "Evet", "common.yes", "common" },
                    { 27, "Hayır", "common.no", "common" },
                    { 30, "Gelen çağrı", "call.incoming", "call" },
                    { 31, "Giden çağrı", "call.outgoing", "call" },
                    { 32, "Beklet", "call.hold", "call" },
                    { 33, "Transfer", "call.transfer", "call" },
                    { 34, "Kapat", "call.hangup", "call" },
                    { 35, "Cevapla", "call.answer", "call" },
                    { 36, "Reddet", "call.reject", "call" },
                    { 40, "Dashboard başlığı", "dashboard.title", "dashboard" },
                    { 41, "Aktif çağrı", "dashboard.active_calls", "dashboard" },
                    { 42, "Online agent", "dashboard.agents_online", "dashboard" },
                    { 43, "Kuyrukta bekleyen", "dashboard.queue_waiting", "dashboard" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "Extension", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "RoleId", "StatusId", "Uid", "UserName" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@callcenter.local", null, "System Admin", true, null, "$2a$11$4NK5QRHYyKGuXY/Wr41bGOgqCOD1PDK.c1473NdyCowy2.HJswS72", 3, 1, new Guid("00000000-0000-0000-0000-000000000001"), "admin" });

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "LanguageCode", "TranslationKeyId", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 1, "tr", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Giriş Yap" },
                    { 2, "en", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Sign In" },
                    { 3, "tr", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Çıkış Yap" },
                    { 4, "en", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Sign Out" },
                    { 5, "tr", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Kullanıcı Adı" },
                    { 6, "en", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Username" },
                    { 7, "tr", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Şifre" },
                    { 8, "en", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Password" },
                    { 9, "tr", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Kullanıcı adı veya şifre hatalı." },
                    { 10, "en", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Invalid username or password." },
                    { 11, "tr", 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Müsait" },
                    { 12, "en", 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Available" },
                    { 13, "tr", 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Meşgul" },
                    { 14, "en", 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Busy" },
                    { 15, "tr", 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Molada" },
                    { 16, "en", 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "On Break" },
                    { 17, "tr", 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Çağrıda" },
                    { 18, "en", 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "In Call" },
                    { 19, "tr", 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Çevrimdışı" },
                    { 20, "en", 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Offline" },
                    { 21, "tr", 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Çağrı Sonrası" },
                    { 22, "en", 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "After Call Work" },
                    { 23, "tr", 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Kaydet" },
                    { 24, "en", 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Save" },
                    { 25, "tr", 21, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "İptal" },
                    { 26, "en", 21, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Cancel" },
                    { 27, "tr", 22, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Sil" },
                    { 28, "en", 22, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Delete" },
                    { 29, "tr", 23, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Düzenle" },
                    { 30, "en", 23, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Edit" },
                    { 31, "tr", 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Ara" },
                    { 32, "en", 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Search" },
                    { 33, "tr", 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Yükleniyor..." },
                    { 34, "en", 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Loading..." },
                    { 35, "tr", 26, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Evet" },
                    { 36, "en", 26, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Yes" },
                    { 37, "tr", 27, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Hayır" },
                    { 38, "en", 27, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "No" },
                    { 39, "tr", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Gelen Çağrı" },
                    { 40, "en", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Incoming Call" },
                    { 41, "tr", 31, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Giden Çağrı" },
                    { 42, "en", 31, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Outgoing Call" },
                    { 43, "tr", 32, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Beklet" },
                    { 44, "en", 32, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Hold" },
                    { 45, "tr", 33, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Transfer" },
                    { 46, "en", 33, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Transfer" },
                    { 47, "tr", 34, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Kapat" },
                    { 48, "en", 34, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Hang Up" },
                    { 49, "tr", 35, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Cevapla" },
                    { 50, "en", 35, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Answer" },
                    { 51, "tr", 36, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Reddet" },
                    { 52, "en", 36, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Reject" },
                    { 53, "tr", 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Gösterge Paneli" },
                    { 54, "en", 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Dashboard" },
                    { 55, "tr", 41, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Aktif Çağrılar" },
                    { 56, "en", 41, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Active Calls" },
                    { 57, "tr", 42, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Çevrimiçi Temsilciler" },
                    { 58, "en", 42, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Agents Online" },
                    { 59, "tr", 43, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Kuyrukta Bekleyen" },
                    { 60, "en", 43, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "Waiting in Queue" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_AgentId",
                table: "CallRecords",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_QueueId",
                table: "CallRecords",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_StartedAt",
                table: "CallRecords",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_Uid",
                table: "CallRecords",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_CustomerId",
                table: "CustomerPersonnel",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_Uid",
                table: "CustomerPersonnel",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_UserId",
                table: "CustomerPersonnel",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_UserTypeId",
                table: "CustomerPersonnel",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelPermissions_CreatedByUserId",
                table: "CustomerPersonnelPermissions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelPermissions_PersonnelId_PermissionTypeId",
                table: "CustomerPersonnelPermissions",
                columns: new[] { "PersonnelId", "PermissionTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPortalModules_CustomerId_ModuleId",
                table: "CustomerPortalModules",
                columns: new[] { "CustomerId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Uid",
                table: "Customers",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserTypePermissions_UserTypeId_PermissionTypeId",
                table: "CustomerUserTypePermissions",
                columns: new[] { "UserTypeId", "PermissionTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserTypes_CustomerId_Name",
                table: "CustomerUserTypes",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserTypes_Uid",
                table: "CustomerUserTypes",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueAgents_AgentId",
                table: "QueueAgents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_CustomerId_Name",
                table: "Queues",
                columns: new[] { "CustomerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Queues_Uid",
                table: "Queues",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SipAccounts_CustomerId",
                table: "SipAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SipAccounts_Uid",
                table: "SipAccounts",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranslationKeys_Key",
                table: "TranslationKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Translations_LanguageCode",
                table: "Translations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_Translations_TranslationKeyId_LanguageCode",
                table: "Translations",
                columns: new[] { "TranslationKeyId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Uid",
                table: "Users",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallRecords");

            migrationBuilder.DropTable(
                name: "CustomerPersonnelPermissions");

            migrationBuilder.DropTable(
                name: "CustomerPortalModules");

            migrationBuilder.DropTable(
                name: "CustomerUserTypePermissions");

            migrationBuilder.DropTable(
                name: "QueueAgents");

            migrationBuilder.DropTable(
                name: "SipAccounts");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Translations");

            migrationBuilder.DropTable(
                name: "CustomerPersonnel");

            migrationBuilder.DropTable(
                name: "Queues");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "TranslationKeys");

            migrationBuilder.DropTable(
                name: "CustomerUserTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
