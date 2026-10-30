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
                    ImageKey = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "para"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Available')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicines", x => x.MedicineId);
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
                    Email = table.Column<string>(type: "longtext", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
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
                    Notification = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "all")
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
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Unread')"),
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
                    IdentityCardNumber = table.Column<string>(type: "longtext", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "varchar(1)", nullable: false),
                    Address = table.Column<string>(type: "longtext", nullable: true),
                    ProfilePicture = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
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
                    StaffId = table.Column<string>(type: "varchar(255)", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
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
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false)
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
                    AppointmentDate = table.Column<DateTime>(type: "date", nullable: false),
                    AppointmentTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Scheduled')"),
                    CheckInStatus = table.Column<string>(type: "longtext", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
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
                    Treatment = table.Column<string>(type: "longtext", nullable: false),
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
                        name: "FK_QueueEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
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
                table: "Counters",
                columns: new[] { "Id", "IsActive", "Key", "Length", "PaddingChar", "Prefix", "Step", "Value", "Version" },
                values: new object[,]
                {
                    { 1L, true, "PatientId", 6, "0", "P", 1, 0L, 0L },
                    { 2L, true, "MedicineId", 4, "0", "MED", 1, 6L, 0L },
                    { 3L, true, "CartId", 4, "0", "CART", 1, 0L, 0L },
                    { 4L, true, "CartItemId", 4, "0", "CARTI", 1, 0L, 0L },
                    { 5L, true, "OrderId", 4, "0", "ORD", 1, 0L, 0L },
                    { 6L, true, "OrderItemId", 4, "0", "ORDI", 1, 0L, 0L },
                    { 7L, true, "InvoiceId", 4, "0", "INV", 1, 0L, 0L },
                    { 8L, true, "PaymentId", 4, "0", "PAY", 1, 0L, 0L },
                    { 9L, true, "StaffId", 6, "0", "STF", 1, 1L, 0L },
                    { 10L, true, "ScheduleId", 6, "0", "SCH", 1, 0L, 0L }
                });

            migrationBuilder.InsertData(
                table: "Medicines",
                columns: new[] { "MedicineId", "Description", "DescriptionZh", "Form", "ImageKey", "Kind", "MedicineName", "Method", "MethodZh", "NameZh", "Pack", "PackZh", "Price", "Status" },
                values: new object[,]
                {
                    { "Med0001", "For a bacterial infection. Finish the course unless the doctor tells you to stop.", "用于细菌感染。除非医生叫停，请把疗程吃完。", "capsule", "amox", "antibiotic", "Amoxicillin 500 mg capsules", "1 capsule, 3 times a day", "每日 3 次，每次 1 粒", "阿莫西林 500 毫克胶囊", "16 capsules", "16 粒", 12.50m, "Available" },
                    { "Med0002", "For fever or pain. Do not take more than 4 g in 24 hours.", "用来退烧或止痛。24 小时内不要超过 4 克。", "tablet", "para", "analgesic", "Paracetamol 500 mg tablets", "1 to 2 tablets, up to 4 times a day", "每次 1 至 2 片，每日最多 4 次", "扑热息痛 500 毫克片", "20 tablets", "20 片", 8.90m, "Available" },
                    { "Med0003", "For allergy symptoms. Take it at the same time each day.", "针对过敏症状。尽量每天同一时间服用。", "tablet", "lora", "antihistamine", "Loratadine 10 mg tablets", "1 tablet once a day", "每日 1 次，每次 1 片", "氯雷他定 10 毫克片", "10 tablets", "10 片", 16.50m, "Available" },
                    { "Med0004", "For pain or swelling after a visit. Take it with food.", "用于就诊后的疼痛或肿。饭后服用。", "tablet", "para", "analgesic", "Ibuprofen 200 mg tablets", "1 tablet, up to 3 times a day", "每次 1 片，每日最多 3 次", "布洛芬 200 毫克片", "24 tablets", "24 片", 9.50m, "Available" },
                    { "Med0005", "For hay fever and itchy skin. It can make some people sleepy.", "用于花粉过敏和皮肤发痒。有些人吃了会困。", "tablet", "lora", "antihistamine", "Cetirizine 10 mg tablets", "1 tablet once a day", "每日 1 次，每次 1 片", "西替利嗪 10 毫克片", "10 tablets", "10 片", 11.00m, "Available" },
                    { "Med0006", "For a skin infection. Take it on an empty stomach and finish the course.", "用于皮肤感染。空腹服用，并把疗程吃完。", "capsule", "amox", "antibiotic", "Cloxacillin 500 mg capsules", "1 capsule, 4 times a day", "每日 4 次，每次 1 粒", "氯唑西林 500 毫克胶囊", "20 capsules", "20 粒", 14.80m, "Available" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Email", "Name", "PasswordHash", "Phone", "Status", "Username" },
                values: new object[] { 1L, "admin1@gmail.com", "Admin One", "AQAAAAIAAYagAAAAEEAius2VoXzeUWqVuzj3ADoMrJRDMTxh9KwW6GCAGnoGB4C5V7uATx2HMRgj1Qp6Ow==", null, "Active", "admin001" });

            migrationBuilder.InsertData(
                table: "Staffs",
                columns: new[] { "StaffId", "Role", "UserId" },
                values: new object[] { "STF000001", "Admin", 1L });

            migrationBuilder.InsertData(
                table: "appSetting",
                columns: new[] { "Id", "Appearance", "Language", "Notification", "UserId" },
                values: new object[] { 1L, "light", null, "all", 1L });

            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "StaffId", "AccessLevel", "Department" },
                values: new object[] { "STF000001", 3, "Administration" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

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
                name: "IX_FamilyMembers_FamilyGroupId",
                table: "FamilyMembers",
                column: "FamilyGroupId");

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
                name: "IX_RoomAssignments_DoctorId",
                table: "RoomAssignments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_RoomId",
                table: "RoomAssignments",
                column: "RoomId");

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
                name: "Counters");

            migrationBuilder.DropTable(
                name: "DispensingRecords");

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
                name: "RoomAssignments");

            migrationBuilder.DropTable(
                name: "WorkingSchedules");

            migrationBuilder.DropTable(
                name: "ShoppingCarts");

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
                name: "ClinicRooms");

            migrationBuilder.DropTable(
                name: "MedicineOrders");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

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
