using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class addBookingAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Appointments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForName",
                table: "Appointments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Appointments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RescheduleCount",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VisitType",
                table: "Appointments",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Counters",
                columns: new[] { "Id", "IsActive", "Key", "Length", "PaddingChar", "Prefix", "Step", "Value", "Version" },
                values: new object[,]
                {
                    { 9L, true, "StaffId", 4, "0", "S", 1, 0L, 0L },
                    { 10L, true, "AppointmentId", 4, "0", "Apt", 1, 0L, 0L },
                    { 11L, true, "ScheduleId", 4, "0", "Sch", 1, 0L, 0L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 11L);

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ForName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduleCount",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VisitType",
                table: "Appointments");
        }
    }
}
