using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class WorkSchedule_room_rommAssg_mcd_mcdBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicRooms",
                columns: table => new
                {
                    RoomId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RoomName = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicRooms", x => x.RoomId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Medicines",
                columns: table => new
                {
                    MedicineId = table.Column<string>(type: "varchar(255)", nullable: false),
                    MedicineName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicines", x => x.MedicineId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkingSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<string>(type: "varchar(255)", nullable: false),
                    StaffId = table.Column<string>(type: "longtext", nullable: false),
                    StaffUserId = table.Column<long>(type: "bigint", nullable: false),
                    WorkingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingSchedules", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_WorkingSchedules_Staffs_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Staffs",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoomAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RoomId = table.Column<string>(type: "varchar(255)", nullable: false),
                    DoctorId = table.Column<string>(type: "longtext", nullable: false),
                    DoctorUserId = table.Column<long>(type: "bigint", nullable: false),
                    AssignmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false)
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
                        name: "FK_RoomAssignments_Doctors_DoctorUserId",
                        column: x => x.DoctorUserId,
                        principalTable: "Doctors",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_MedicineId",
                table: "MedicineBatches",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_DoctorUserId",
                table: "RoomAssignments",
                column: "DoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_RoomId",
                table: "RoomAssignments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingSchedules_StaffUserId",
                table: "WorkingSchedules",
                column: "StaffUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineBatches");

            migrationBuilder.DropTable(
                name: "RoomAssignments");

            migrationBuilder.DropTable(
                name: "WorkingSchedules");

            migrationBuilder.DropTable(
                name: "Medicines");

            migrationBuilder.DropTable(
                name: "ClinicRooms");
        }
    }
}
