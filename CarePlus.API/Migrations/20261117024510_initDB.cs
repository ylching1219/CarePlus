using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class initDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    LogId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Module = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    AffectedRecord = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.LogId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClinicRooms",
                columns: table => new
                {
                    RoomId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RoomName = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Available')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicRooms", x => x.RoomId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Counters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Prefix = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    Length = table.Column<int>(type: "int", nullable: false),
                    PaddingChar = table.Column<string>(type: "varchar(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counters", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Medicines",
                columns: table => new
                {
                    MedicineId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicineName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    NameZh = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    DescriptionZh = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Pack = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValue: ""),
                    PackZh = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    Form = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "tablet"),
                    Kind = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "analgesic"),
                    Method = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false, defaultValue: ""),
                    MethodZh = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    ImagePath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiresPrescription = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Available')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicines", x => x.MedicineId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RegistrationEmailChallenges",
                columns: table => new
                {
                    ChallengeId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false),
                    SecretHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationEmailChallenges", x => x.ChallengeId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    EmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    FailedPasswordAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PasswordLockoutUntilUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SessionVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VisitTypes",
                columns: table => new
                {
                    VisitTypeId = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitTypes", x => x.VisitTypeId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MedicineBatches",
                columns: table => new
                {
                    BatchId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicineId = table.Column<string>(type: "varchar(255)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineBatches", x => x.BatchId);
                    table.ForeignKey(
                        name: "FK_MedicineBatches_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appSetting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Appearance = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "light"),
                    Language = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true),
                    Notification = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "all"),
                    TextSize = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    HasCompletedTutorial = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appSetting_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LoginActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Device = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<string>(type: "varchar(255)", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false),
                    Type = table.Column<string>(type: "varchar(255)", nullable: false),
                    Actor = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "CarePlus Clinic"),
                    Initials = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false, defaultValue: "CC"),
                    TitleZh = table.Column<string>(type: "longtext", nullable: true),
                    MessageZh = table.Column<string>(type: "longtext", nullable: true),
                    Href = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, defaultValue: "/notifications"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "('Unread')"),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IdentityCardNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "varchar(1)", nullable: false),
                    Address = table.Column<string>(type: "longtext", nullable: true),
                    ProfilePicture = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    AnnouncementStartAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_Patients_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "paymentCard",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Brand = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Last4 = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    Expiry = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    HolderName = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false),
                    NumberHash = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    CvvHash = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paymentCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paymentCard_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    StaffId = table.Column<string>(type: "varchar(255)", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Staffs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TwoFactorChallenges",
                columns: table => new
                {
                    ChallengeId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Purpose = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    CodeHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwoFactorChallenges", x => x.ChallengeId);
                    table.ForeignKey(
                        name: "FK_TwoFactorChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChatThreads",
                columns: table => new
                {
                    ChatThreadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PatientId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatThreads", x => x.ChatThreadId);
                    table.ForeignKey(
                        name: "FK_ChatThreads_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeliveryAddresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Label = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Unit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Latitude = table.Column<double>(type: "double", nullable: true),
                    Longitude = table.Column<double>(type: "double", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryAddresses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FamilyGroups",
                columns: table => new
                {
                    FamilyGroupId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GroupName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedBy = table.Column<string>(type: "varchar(255)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyGroups", x => x.FamilyGroupId);
                    table.ForeignKey(
                        name: "FK_FamilyGroups_Patients_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PersonalHealthRecords",
                columns: table => new
                {
                    HealthRecId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RecordType = table.Column<string>(type: "longtext", nullable: false),
                    BloodPressure = table.Column<string>(type: "longtext", nullable: true),
                    Allergies = table.Column<string>(type: "longtext", nullable: true),
                    HealthConditions = table.Column<string>(type: "longtext", nullable: true),
                    MedicalHistory = table.Column<string>(type: "longtext", nullable: true),
                    Symptoms = table.Column<string>(type: "longtext", nullable: true),
                    HealthNotes = table.Column<string>(type: "longtext", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PdfFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    PdfStorageName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalHealthRecords", x => x.HealthRecId);
                    table.ForeignKey(
                        name: "FK_PersonalHealthRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShoppingCarts",
                columns: table => new
                {
                    CartId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingCarts", x => x.CartId);
                    table.ForeignKey(
                        name: "FK_ShoppingCarts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    StaffId = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Admins_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    StaffId = table.Column<string>(type: "varchar(255)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Specialization = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Qualification = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    ProfilePicture = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Doctors_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeaveApplications",
                columns: table => new
                {
                    LeaveId = table.Column<string>(type: "varchar(255)", nullable: false),
                    StaffId = table.Column<string>(type: "varchar(255)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: false),
                    SupportDocument = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveApplications", x => x.LeaveId);
                    table.ForeignKey(
                        name: "FK_LeaveApplications_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pharmacists",
                columns: table => new
                {
                    StaffId = table.Column<string>(type: "varchar(255)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Qualification = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Certification = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacists", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Pharmacists_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkingSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<string>(type: "varchar(255)", nullable: false),
                    StaffId = table.Column<string>(type: "varchar(255)", nullable: false),
                    WorkingDate = table.Column<DateTime>(type: "date", nullable: false),
                    Shift = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Morning"),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Active')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingSchedules", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_WorkingSchedules_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    ChatMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ChatThreadId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    Disclaimer = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    SourcesJson = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.ChatMessageId);
                    table.CheckConstraint("CK_ChatMessages_Role", "`Role` IN ('user', 'assistant')");
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatThreads_ChatThreadId",
                        column: x => x.ChatThreadId,
                        principalTable: "ChatThreads",
                        principalColumn: "ChatThreadId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MedicineOrders",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    OrderDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')"),
                    Fulfilment = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "pickup"),
                    PaymentMethod = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, defaultValue: ""),
                    DeliveryAddressId = table.Column<long>(type: "bigint", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "double", nullable: true),
                    Longitude = table.Column<double>(type: "double", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_MedicineOrders_DeliveryAddresses_DeliveryAddressId",
                        column: x => x.DeliveryAddressId,
                        principalTable: "DeliveryAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MedicineOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    FamilyMemberId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FamilyGroupId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Relationship = table.Column<string>(type: "longtext", nullable: false),
                    AccessLevel = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('View')"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    JoinAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.FamilyMemberId);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_FamilyGroups_FamilyGroupId",
                        column: x => x.FamilyGroupId,
                        principalTable: "FamilyGroups",
                        principalColumn: "FamilyGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    CartItemId = table.Column<string>(type: "varchar(255)", nullable: false),
                    CartId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicineId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.CartItemId);
                    table.ForeignKey(
                        name: "FK_CartItems_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_ShoppingCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "ShoppingCarts",
                        principalColumn: "CartId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    DoctorId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RoomId = table.Column<string>(type: "varchar(255)", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "date", nullable: false),
                    AppointmentTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ForName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    VisitType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    RescheduleCount = table.Column<int>(type: "int", nullable: false),
                    LastRescheduledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Scheduled')"),
                    CheckInStatus = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_Appointments_ClinicRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "ClinicRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MedicalRecords",
                columns: table => new
                {
                    MedicalRecordId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    DoctorId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ConsultationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Diagnosis = table.Column<string>(type: "longtext", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecords", x => x.MedicalRecordId);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoomAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RoomId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ScheduleId = table.Column<string>(type: "varchar(255)", nullable: true),
                    DoctorId = table.Column<string>(type: "varchar(255)", nullable: false),
                    AssignmentDate = table.Column<DateTime>(type: "date", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_RoomAssignments_ClinicRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "ClinicRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomAssignments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomAssignments_WorkingSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "WorkingSchedules",
                        principalColumn: "ScheduleId",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    OrderItemId = table.Column<string>(type: "varchar(255)", nullable: false),
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicineId = table.Column<string>(type: "varchar(255)", nullable: true),
                    ItemName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false, defaultValue: ""),
                    Detail = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_OrderItems_MedicineOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MedicineOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QueueEntries",
                columns: table => new
                {
                    QueueId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    AppointmentId = table.Column<string>(type: "varchar(255)", nullable: true),
                    RoomId = table.Column<string>(type: "varchar(255)", nullable: true),
                    QueueType = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Waiting')"),
                    CheckInTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueEntries", x => x.QueueId);
                    table.ForeignKey(
                        name: "FK_QueueEntries_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId");
                    table.ForeignKey(
                        name: "FK_QueueEntries_ClinicRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "ClinicRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QueueEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DoctorReviews",
                columns: table => new
                {
                    MedicalRecordId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorReviews", x => x.MedicalRecordId);
                    table.CheckConstraint("CK_DoctorReviews_Rating", "`Rating` BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_DoctorReviews_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "MedicalRecordId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    InvoiceId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicalRecordId = table.Column<string>(type: "varchar(255)", nullable: true),
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssueAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: true),
                    DoctorName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    VisitType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    RecordRef = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    PaymentStatus = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_Invoices_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "MedicalRecordId");
                    table.ForeignKey(
                        name: "FK_Invoices_MedicineOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MedicineOrders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_Invoices_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    PrescriptionId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    DoctorId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicalRecordId = table.Column<string>(type: "varchar(255)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.PrescriptionId);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prescriptions_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "MedicalRecordId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<string>(type: "varchar(255)", nullable: false),
                    InvoiceId = table.Column<string>(type: "varchar(255)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "longtext", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DispensingRecords",
                columns: table => new
                {
                    DispensingId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PrescriptionId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PharmacistId = table.Column<string>(type: "varchar(255)", nullable: false),
                    DispenseAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Dispensed')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispensingRecords", x => x.DispensingId);
                    table.ForeignKey(
                        name: "FK_DispensingRecords_Pharmacists_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "Pharmacists",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispensingRecords_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "PrescriptionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    PrescriptionItemId = table.Column<string>(type: "varchar(255)", nullable: false),
                    PrescriptionId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicineId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Dosage = table.Column<string>(type: "longtext", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.PrescriptionItemId);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "PrescriptionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ClinicRooms",
                columns: new[] { "RoomId", "RoomName", "Status" },
                values: new object[,]
                {
                    { "RM-0001", "Consultation Room 1", "Available" },
                    { "RM-0002", "Consultation Room 2", "Available" },
                    { "RM-0003", "Consultation Room 3", "Available" },
                    { "RM-0004", "Consultation Room 4", "Available" },
                    { "RM-0005", "Consultation Room 5", "Available" }
                });

            migrationBuilder.InsertData(
                table: "Counters",
                columns: new[] { "Id", "IsActive", "Key", "Length", "PaddingChar", "Prefix", "Step", "Value", "Version" },
                values: new object[,]
                {
                    { 1L, true, "PatientId", 6, "0", "P", 1, 9L, 9L },
                    { 2L, true, "MedicineId", 4, "0", "MED", 1, 6L, 6L },
                    { 3L, true, "CartId", 4, "0", "CART", 1, 0L, 0L },
                    { 4L, true, "CartItemId", 4, "0", "CARTI", 1, 0L, 0L },
                    { 5L, true, "OrderId", 4, "0", "ORD", 1, 0L, 0L },
                    { 6L, true, "OrderItemId", 4, "0", "ORDI", 1, 0L, 0L },
                    { 7L, true, "InvoiceId", 4, "0", "INV", 1, 1L, 1L },
                    { 8L, true, "PaymentId", 4, "0", "PAY", 1, 0L, 0L },
                    { 9L, true, "StaffId", 6, "0", "STF", 1, 9L, 9L },
                    { 10L, true, "AppointmentId", 4, "0", "APT", 1, 0L, 0L },
                    { 11L, true, "ScheduleId", 6, "0", "SCH", 1, 9L, 9L },
                    { 12L, true, "RoomId", 4, "0", "RM-", 1, 5L, 0L },
                    { 13L, true, "PrescriptionId", 4, "0", "RX", 1, 0L, 0L },
                    { 14L, true, "PrescriptionItemId", 4, "0", "RXI", 1, 0L, 0L }
                });

            migrationBuilder.InsertData(
                table: "Medicines",
                columns: new[] { "MedicineId", "Description", "DescriptionZh", "Form", "ImagePath", "Kind", "MedicineName", "Method", "MethodZh", "NameZh", "Pack", "PackZh", "Price", "RequiresPrescription", "Status" },
                values: new object[,]
                {
                    { "MED0001", "For a bacterial infection. Finish the course unless the doctor tells you to stop.", "用于细菌感染。除非医生叫停，请把疗程吃完。", "capsule", "medicines/med0001.jpg", "antibiotic", "Amoxicillin 500 mg capsules", "1 capsule, 3 times a day", "每日 3 次，每次 1 粒", "阿莫西林 500 毫克胶囊", "16 capsules", "16 粒", 12.50m, false, "Available" },
                    { "MED0002", "For fever or pain. Do not take more than 4 g in 24 hours.", "用来退烧或止痛。24 小时内不要超过 4 克。", "tablet", "medicines/med0002.jpg", "analgesic", "Paracetamol 500 mg tablets", "1 to 2 tablets, up to 4 times a day", "每次 1 至 2 片，每日最多 4 次", "扑热息痛 500 毫克片", "20 tablets", "20 片", 8.90m, false, "Available" },
                    { "MED0003", "For allergy symptoms. Take it at the same time each day.", "针对过敏症状。尽量每天同一时间服用。", "tablet", "medicines/med0003.jpg", "antihistamine", "Loratadine 10 mg tablets", "1 tablet once a day", "每日 1 次，每次 1 片", "氯雷他定 10 毫克片", "10 tablets", "10 片", 16.50m, false, "Available" },
                    { "MED0004", "For pain or swelling after a visit. Take it with food.", "用于就诊后的疼痛或肿。饭后服用。", "tablet", "medicines/med0004.jpg", "analgesic", "Ibuprofen 200 mg tablets", "1 tablet, up to 3 times a day", "每次 1 片，每日最多 3 次", "布洛芬 200 毫克片", "24 tablets", "24 片", 9.50m, false, "Available" },
                    { "MED0005", "For hay fever and itchy skin. It can make some people sleepy.", "用于花粉过敏和皮肤发痒。有些人吃了会困。", "tablet", "medicines/med0005.jpg", "antihistamine", "Cetirizine 10 mg tablets", "1 tablet once a day", "每日 1 次，每次 1 片", "西替利嗪 10 毫克片", "10 tablets", "10 片", 11.00m, false, "Available" },
                    { "MED0006", "For a skin infection. Take it on an empty stomach and finish the course.", "用于皮肤感染。空腹服用，并把疗程吃完。", "capsule", "medicines/med0006.jpg", "antibiotic", "Cloxacillin 500 mg capsules", "1 capsule, 4 times a day", "每日 4 次，每次 1 粒", "氯唑西林 500 毫克胶囊", "20 capsules", "20 粒", 14.80m, false, "Available" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Email", "EmailVerified", "Name", "PasswordHash", "PasswordLockoutUntilUtc", "Phone", "SessionVersion", "Status", "Username" },
                values: new object[,]
                {
                    { 1L, "admin@careplus.local", true, "Nurul Aisyah Rahman", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001001", 0, "Active", "admin" },
                    { 2L, "lim.weijie@careplus.local", true, "Lim Wei Jie", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001002", 0, "Active", "drlim" },
                    { 3L, "siti.aminah@careplus.local", true, "Siti Aminah Hassan", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001003", 0, "Active", "drsiti" },
                    { 4L, "arjun.menon@careplus.local", true, "Arjun Menon", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001004", 0, "Active", "drarjun" },
                    { 5L, "tan.meiling@careplus.local", true, "Tan Mei Ling", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001005", 0, "Active", "drtan" },
                    { 6L, "hafiz.osman@careplus.local", true, "Hafiz bin Osman", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001006", 0, "Active", "hafiz" },
                    { 7L, "chloe.ng@careplus.local", true, "Chloe Ng", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001007", 0, "Active", "chloe" },
                    { 8L, "priya.krishnan@careplus.local", true, "Priya Krishnan", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001008", 0, "Active", "priya" },
                    { 9L, "daniel.chong@careplus.local", true, "Daniel Chong", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123001009", 0, "Active", "daniel" },
                    { 10L, "ahmad.farid@careplus.local", true, "Ahmad Farid bin Zulkifli", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002001", 0, "Active", "ahmad" },
                    { 11L, "lina.teoh@careplus.local", true, "Lina Teoh", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002002", 0, "Active", "lina" },
                    { 12L, "kumar.rajan@careplus.local", true, "Kumar a/l Rajan", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002003", 0, "Active", "kumar" },
                    { 13L, "grace.wong@careplus.local", true, "Grace Wong", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002004", 0, "Active", "grace" },
                    { 14L, "aina.ahmad@careplus.local", true, "Aina Ahmad", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002005", 0, "Active", "aina" },
                    { 15L, "adam.ahmad@careplus.local", true, "Adam Ahmad", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002006", 0, "Active", "adam" },
                    { 16L, "hana.kumar@careplus.local", true, "Hana Kumar", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002007", 0, "Active", "hana" },
                    { 17L, "lucas.teoh@careplus.local", true, "Lucas Teoh", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002008", 0, "Active", "lucas" },
                    { 18L, "farah.ismail@careplus.local", true, "Farah Ismail", "AQAAAAIAAYagAAAAEN7npY5nsp8JgmC7oobqdViptrmJ0wM0pHgSu40u8GhflW/Ol8TjEPTZchg3CgdBMg==", null, "0123002009", 0, "Active", "farah" }
                });

            migrationBuilder.InsertData(
                table: "VisitTypes",
                columns: new[] { "VisitTypeId", "Description", "IsActive", "Name", "Price" },
                values: new object[,]
                {
                    { "VT01", "Cough, fever, skin, or a usual GP visit.", true, "General consultation", 50m },
                    { "VT02", "Health screening and routine checks.", true, "Body check", 55m },
                    { "VT03", "Stress, sleep, mood, or a quiet talk with a doctor.", true, "Mental wellness", 60m }
                });

            migrationBuilder.InsertData(
                table: "MedicineBatches",
                columns: new[] { "BatchId", "ExpiryDate", "MedicineId", "StockQuantity" },
                values: new object[,]
                {
                    { "BAT-20261101090000-MED0001", new DateTime(2027, 5, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "MED0001", 80 },
                    { "BAT-20261101090100-MED0002", new DateTime(2027, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "MED0002", 120 },
                    { "BAT-20261101090200-MED0003", new DateTime(2027, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "MED0003", 60 },
                    { "BAT-20261101090300-MED0004", new DateTime(2027, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "MED0004", 96 },
                    { "BAT-20261101090400-MED0005", new DateTime(2027, 8, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "MED0005", 60 },
                    { "BAT-20261101090500-MED0006", new DateTime(2027, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "MED0006", 80 }
                });

            migrationBuilder.InsertData(
                table: "Staffs",
                columns: new[] { "StaffId", "Role", "UserId" },
                values: new object[,]
                {
                    { "STF000001", "Admin", 1L },
                    { "STF000002", "Doctor", 2L },
                    { "STF000003", "Doctor", 3L },
                    { "STF000004", "Doctor", 4L },
                    { "STF000005", "Doctor", 5L },
                    { "STF000006", "Pharmacist", 6L },
                    { "STF000007", "Pharmacist", 7L },
                    { "STF000008", "Pharmacist", 8L },
                    { "STF000009", "Pharmacist", 9L }
                });

            migrationBuilder.InsertData(
                table: "appSetting",
                columns: new[] { "Id", "Appearance", "Language", "Notification", "TextSize", "UserId" },
                values: new object[] { 1L, "dark", null, "all", "normal", 1L });

            migrationBuilder.InsertData(
                table: "Admins",
                column: "StaffId",
                value: "STF000001");

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "StaffId", "LicenseNumber", "ProfilePicture", "Qualification", "Specialization", "YearsOfExperience" },
                values: new object[,]
                {
                    { "STF000002", "DOC-LIC-0001", "profiles/doctor-doctor001.png", "MBBS", "General Medicine", 8 },
                    { "STF000003", "DOC-LIC-0002", "profiles/doctor-doctor002.jpg", "MBBS", "Family Medicine", 11 },
                    { "STF000004", "DOC-LIC-0003", "profiles/doctor-doctor003.jpg", "MD", "Internal Medicine", 14 },
                    { "STF000005", "DOC-LIC-0004", "profiles/doctor-doctor004.jpg", "MBBS", "Paediatrics", 6 }
                });

            migrationBuilder.InsertData(
                table: "Pharmacists",
                columns: new[] { "StaffId", "Certification", "LicenseNumber", "Qualification", "YearsOfExperience" },
                values: new object[,]
                {
                    { "STF000006", "Registered Pharmacist", "PHA-LIC-0001", "Bachelor of Pharmacy", 7 },
                    { "STF000007", "Registered Pharmacist", "PHA-LIC-0002", "Bachelor of Pharmacy", 5 },
                    { "STF000008", "Registered Pharmacist", "PHA-LIC-0003", "Diploma in Pharmacy", 3 },
                    { "STF000009", "Registered Pharmacist", "PHA-LIC-0004", "Bachelor of Pharmacy", 9 }
                });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "PatientId", "UserId", "IdentityCardNumber", "DateOfBirth", "Gender", "Address", "ProfilePicture", "AnnouncementStartAt" },
                values: new object[,]
                {
                    { "P000001", 10L, "880412145521", new DateTime(1988, 4, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "M", "12 Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur", null, null },
                    { "P000002", 11L, "900903105588", new DateTime(1990, 9, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "F", "12 Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur", null, null },
                    { "P000003", 12L, "850120085511", new DateTime(1985, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "M", "88 Jalan Gombak, 53000 Kuala Lumpur", null, null },
                    { "P000004", 13L, "921108145533", new DateTime(1992, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "F", "21 Jalan Ampang, 50450 Kuala Lumpur", null, null },
                    { "P000005", 14L, "180615145001", new DateTime(2018, 6, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "F", "12 Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur", null, null },
                    { "P000006", 15L, "200202145002", new DateTime(2020, 2, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "M", "12 Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur", null, null },
                    { "P000007", 16L, "170821085003", new DateTime(2017, 8, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "F", "88 Jalan Gombak, 53000 Kuala Lumpur", null, null },
                    { "P000008", 17L, "191201145004", new DateTime(2019, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "M", "21 Jalan Ampang, 50450 Kuala Lumpur", null, null },
                    { "P000009", 18L, "910325105599", new DateTime(1991, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "F", "5 Jalan Pahang, 53000 Kuala Lumpur", null, null }
                });

            migrationBuilder.InsertData(
                table: "FamilyGroups",
                columns: new[] { "FamilyGroupId", "GroupName", "MemberCount", "CreatedBy", "CreatedAt" },
                values: new object[] { 1L, "Ahmad family", 4, "P000001", new DateTime(2026, 11, 1, 9, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "FamilyMembers",
                columns: new[] { "FamilyMemberId", "FamilyGroupId", "PatientId", "Relationship", "AccessLevel", "DisplayName", "JoinAt", "Status" },
                values: new object[,]
                {
                    { 1L, 1L, "P000001", "parent", "Parent", "Ahmad Farid", new DateTime(2026, 11, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Active" },
                    { 2L, 1L, "P000002", "parent", "Parent", "Lina Teoh", new DateTime(2026, 11, 1, 9, 5, 0, 0, DateTimeKind.Unspecified), "Active" },
                    { 3L, 1L, "P000005", "child", "Child", "Aina Ahmad", new DateTime(2026, 11, 1, 9, 10, 0, 0, DateTimeKind.Unspecified), "Active" },
                    { 4L, 1L, "P000006", "child", "Child", "Adam Ahmad", new DateTime(2026, 11, 1, 9, 10, 0, 0, DateTimeKind.Unspecified), "Active" }
                });

            migrationBuilder.InsertData(
                table: "WorkingSchedules",
                columns: new[] { "ScheduleId", "StaffId", "WorkingDate", "Shift", "Status" },
                values: new object[,]
                {
                    { "SCH000001", "STF000002", new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000002", "STF000002", new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000003", "STF000002", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000004", "STF000002", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000005", "STF000002", new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000006", "STF000002", new DateTime(2026, 11, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000007", "STF000002", new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000008", "STF000002", new DateTime(2026, 11, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" },
                    { "SCH000009", "STF000002", new DateTime(2026, 11, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Morning", "Active" }
                });

            migrationBuilder.InsertData(
                table: "RoomAssignments",
                columns: new[] { "AssignmentId", "RoomId", "ScheduleId", "DoctorId", "AssignmentDate", "StartTime", "EndTime" },
                values: new object[,]
                {
                    { "RA-00000001", "RM-0001", "SCH000001", "STF000002", new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000002", "RM-0001", "SCH000002", "STF000002", new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000003", "RM-0001", "SCH000003", "STF000002", new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000004", "RM-0001", "SCH000004", "STF000002", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000005", "RM-0001", "SCH000005", "STF000002", new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000006", "RM-0001", "SCH000006", "STF000002", new DateTime(2026, 11, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000007", "RM-0001", "SCH000007", "STF000002", new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000008", "RM-0001", "SCH000008", "STF000002", new DateTime(2026, 11, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) },
                    { "RA-00000009", "RM-0001", "SCH000009", "STF000002", new DateTime(2026, 11, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "Invoices",
                columns: new[] { "InvoiceId", "PatientId", "MedicalRecordId", "OrderId", "TotalAmount", "IssueAt", "DueDate", "DoctorName", "VisitType", "RecordRef", "PaymentStatus" },
                values: new object[] { "INV0001", "P000009", null, null, 50.00m, new DateTime(2026, 10, 31, 10, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 11, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "Lim Wei Jie", "General consultation", null, "Pending" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RoomId",
                table: "Appointments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_appSetting_UserId",
                table: "appSetting",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DateTime",
                table: "AuditLogs",
                column: "DateTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Module_Action",
                table: "AuditLogs",
                columns: new[] { "Module", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_MedicineId",
                table: "CartItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatThreadId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "ChatThreadId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatThreads_PatientId_UpdatedAt",
                table: "ChatThreads",
                columns: new[] { "PatientId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Counters_Key",
                table: "Counters",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAddresses_PatientId",
                table: "DeliveryAddresses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DispensingRecords_PharmacistId",
                table: "DispensingRecords",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_DispensingRecords_PrescriptionId",
                table: "DispensingRecords",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_LicenseNumber",
                table: "Doctors",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyGroups_CreatedBy",
                table: "FamilyGroups",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_FamilyGroupId_PatientId",
                table: "FamilyMembers",
                columns: new[] { "FamilyGroupId", "PatientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_PatientId",
                table: "FamilyMembers",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MedicalRecordId",
                table: "Invoices",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PatientId",
                table: "Invoices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApplications_StaffId",
                table: "LeaveApplications",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_UserId",
                table: "LoginActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_DoctorId",
                table: "MedicalRecords",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_MedicineId",
                table: "MedicineBatches",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineOrders_DeliveryAddressId",
                table: "MedicineOrders",
                column: "DeliveryAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineOrders_PatientId",
                table: "MedicineOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "Type", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MedicineId",
                table: "OrderItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paymentCard_UserId",
                table: "paymentCard",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalHealthRecords_PatientId",
                table: "PersonalHealthRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacists_LicenseNumber",
                table: "Pharmacists",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_MedicineId",
                table: "PrescriptionItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_MedicalRecordId",
                table: "Prescriptions",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_AppointmentId",
                table: "QueueEntries",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_PatientId",
                table: "QueueEntries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_RoomId",
                table: "QueueEntries",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEmailChallenges_Email_CreatedAt",
                table: "RegistrationEmailChallenges",
                columns: new[] { "Email", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_DoctorId",
                table: "RoomAssignments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_RoomId",
                table: "RoomAssignments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_ScheduleId",
                table: "RoomAssignments",
                column: "ScheduleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_PatientId",
                table: "ShoppingCarts",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_UserId",
                table: "Staffs",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorChallenges_UserId_Purpose_CreatedAt",
                table: "TwoFactorChallenges",
                columns: new[] { "UserId", "Purpose", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkingSchedules_StaffId",
                table: "WorkingSchedules",
                column: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "appSetting");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Counters");

            migrationBuilder.DropTable(
                name: "DispensingRecords");

            migrationBuilder.DropTable(
                name: "DoctorReviews");

            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "LeaveApplications");

            migrationBuilder.DropTable(
                name: "LoginActivities");

            migrationBuilder.DropTable(
                name: "MedicineBatches");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "paymentCard");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PersonalHealthRecords");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "QueueEntries");

            migrationBuilder.DropTable(
                name: "RegistrationEmailChallenges");

            migrationBuilder.DropTable(
                name: "RoomAssignments");

            migrationBuilder.DropTable(
                name: "TwoFactorChallenges");

            migrationBuilder.DropTable(
                name: "VisitTypes");

            migrationBuilder.DropTable(
                name: "ShoppingCarts");

            migrationBuilder.DropTable(
                name: "ChatThreads");

            migrationBuilder.DropTable(
                name: "Pharmacists");

            migrationBuilder.DropTable(
                name: "FamilyGroups");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Medicines");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "WorkingSchedules");

            migrationBuilder.DropTable(
                name: "MedicineOrders");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "ClinicRooms");

            migrationBuilder.DropTable(
                name: "DeliveryAddresses");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
