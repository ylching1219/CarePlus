using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class patientPortalFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Medicines_MedicineId",
                table: "OrderItems");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WorkingSchedules",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Active')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "QueueEntries",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Waiting')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payments",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Payments",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Patients",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityCardNumber",
                table: "Patients",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "Patients",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MedicineId",
                table: "OrderItems",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.AddColumn<string>(
                name: "Detail",
                table: "OrderItems",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "OrderItems",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Notifications",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Unread')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Medicines",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Available')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Medicines",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionZh",
                table: "Medicines",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Form",
                table: "Medicines",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "tablet");

            migrationBuilder.AddColumn<string>(
                name: "ImageKey",
                table: "Medicines",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "para");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Medicines",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "analgesic");

            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "Medicines",
                type: "varchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MethodZh",
                table: "Medicines",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameZh",
                table: "Medicines",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pack",
                table: "Medicines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PackZh",
                table: "Medicines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MedicineOrders",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "MedicineOrders",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeliveryAddressId",
                table: "MedicineOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "MedicineOrders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Fulfilment",
                table: "MedicineOrders",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "pickup");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "MedicineOrders",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "MedicineOrders",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "MedicineOrders",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "LeaveApplications",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<string>(
                name: "DoctorName",
                table: "Invoices",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordRef",
                table: "Invoices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitType",
                table: "Invoices",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FamilyMembers",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "AccessLevel",
                table: "FamilyMembers",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('View')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<int>(
                name: "MemberCount",
                table: "FamilyGroups",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DispensingRecords",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Dispensed')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Prefix",
                table: "Counters",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Counters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClinicRooms",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Available')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appointments",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Scheduled')",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "CheckInStatus",
                table: "Appointments",
                type: "longtext",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "longtext");

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

            migrationBuilder.InsertData(
                table: "Counters",
                columns: new[] { "Id", "IsActive", "Key", "Length", "PaddingChar", "Prefix", "Step", "Value", "Version" },
                values: new object[,]
                {
                    { 1L, true, "PatientId", 6, "0", "P", 1, 0L, 0L },
                    { 2L, true, "MedicineId", 4, "0", "Med", 1, 6L, 0L },
                    { 3L, true, "CartId", 4, "0", "Cart", 1, 0L, 0L },
                    { 4L, true, "CartItemId", 4, "0", "CartI", 1, 0L, 0L },
                    { 5L, true, "OrderId", 4, "0", "Ord", 1, 0L, 0L },
                    { 6L, true, "OrderItemId", 4, "0", "OrdI", 1, 0L, 0L },
                    { 7L, true, "InvoiceId", 4, "0", "Inv", 1, 0L, 0L },
                    { 8L, true, "PaymentId", 4, "0", "Pay", 1, 0L, 0L }
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

            migrationBuilder.CreateIndex(
                name: "IX_MedicineOrders_DeliveryAddressId",
                table: "MedicineOrders",
                column: "DeliveryAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_appSetting_UserId",
                table: "appSetting",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAddresses_PatientId",
                table: "DeliveryAddresses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_UserId",
                table: "LoginActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_paymentCard_UserId",
                table: "paymentCard",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineOrders_DeliveryAddresses_DeliveryAddressId",
                table: "MedicineOrders",
                column: "DeliveryAddressId",
                principalTable: "DeliveryAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Medicines_MedicineId",
                table: "OrderItems",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "MedicineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineOrders_DeliveryAddresses_DeliveryAddressId",
                table: "MedicineOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Medicines_MedicineId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "appSetting");

            migrationBuilder.DropTable(
                name: "DeliveryAddresses");

            migrationBuilder.DropTable(
                name: "LoginActivities");

            migrationBuilder.DropTable(
                name: "paymentCard");

            migrationBuilder.DropIndex(
                name: "IX_MedicineOrders_DeliveryAddressId",
                table: "MedicineOrders");

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: "Med0001");

            migrationBuilder.DeleteData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: "Med0002");

            migrationBuilder.DeleteData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: "Med0003");

            migrationBuilder.DeleteData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: "Med0004");

            migrationBuilder.DeleteData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: "Med0005");

            migrationBuilder.DeleteData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: "Med0006");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IdentityCardNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Detail",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DescriptionZh",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Form",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "ImageKey",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "MethodZh",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "NameZh",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Pack",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "PackZh",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "MedicineOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressId",
                table: "MedicineOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "MedicineOrders");

            migrationBuilder.DropColumn(
                name: "Fulfilment",
                table: "MedicineOrders");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "MedicineOrders");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "MedicineOrders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "MedicineOrders");

            migrationBuilder.DropColumn(
                name: "DoctorName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RecordRef",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VisitType",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WorkingSchedules",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Active')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "QueueEntries",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Waiting')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payments",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Patients",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MedicineId",
                table: "OrderItems",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Notifications",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Unread')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Medicines",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Available')");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Medicines",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MedicineOrders",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "LeaveApplications",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FamilyMembers",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<string>(
                name: "AccessLevel",
                table: "FamilyMembers",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('View')");

            migrationBuilder.AlterColumn<int>(
                name: "MemberCount",
                table: "FamilyGroups",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DispensingRecords",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Dispensed')");

            migrationBuilder.AlterColumn<string>(
                name: "Prefix",
                table: "Counters",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Counters",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClinicRooms",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Available')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appointments",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Scheduled')");

            migrationBuilder.AlterColumn<string>(
                name: "CheckInStatus",
                table: "Appointments",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Medicines_MedicineId",
                table: "OrderItems",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "MedicineId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
