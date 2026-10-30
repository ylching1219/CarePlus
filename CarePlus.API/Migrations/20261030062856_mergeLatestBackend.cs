using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class mergeLatestBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Shift",
                table: "WorkingSchedules",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Morning");

            migrationBuilder.Sql("""
                UPDATE `WorkingSchedules`
                SET `Shift` = CASE
                    WHEN HOUR(`StartTime`) >= 18 OR HOUR(`StartTime`) < 6 THEN 'Night'
                    ELSE 'Morning'
                END;
                """);

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "WorkingSchedules");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "WorkingSchedules");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeleteDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Treatment",
                table: "MedicalRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LeaveApplications",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<string>(
                name: "SupportDocument",
                table: "LeaveApplications",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 2L,
                column: "Prefix",
                value: "MED");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 3L,
                column: "Prefix",
                value: "CART");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 4L,
                column: "Prefix",
                value: "CARTI");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 5L,
                column: "Prefix",
                value: "ORD");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 6L,
                column: "Prefix",
                value: "ORDI");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 7L,
                column: "Prefix",
                value: "INV");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 8L,
                column: "Prefix",
                value: "PAY");

            migrationBuilder.Sql("""
                UPDATE `Counters`
                SET `Length` = 6,
                    `Prefix` = 'STF',
                    `Value` = GREATEST(`Value`, 1)
                WHERE `Id` = 9;
                """);

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 11L,
                columns: new[] { "Length", "Prefix" },
                values: new object[] { 6, "SCH" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DateTime",
                table: "AuditLogs",
                column: "DateTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Module_Action",
                table: "AuditLogs",
                columns: new[] { "Module", "Action" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Shift",
                table: "WorkingSchedules");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LeaveApplications");

            migrationBuilder.DropColumn(
                name: "SupportDocument",
                table: "LeaveApplications");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "WorkingSchedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "WorkingSchedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteDate",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Treatment",
                table: "MedicalRecords",
                type: "longtext",
                nullable: false);

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 2L,
                column: "Prefix",
                value: "Med");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 3L,
                column: "Prefix",
                value: "Cart");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 4L,
                column: "Prefix",
                value: "CartI");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 5L,
                column: "Prefix",
                value: "Ord");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 6L,
                column: "Prefix",
                value: "OrdI");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 7L,
                column: "Prefix",
                value: "Inv");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 8L,
                column: "Prefix",
                value: "Pay");

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 9L,
                columns: new[] { "Length", "Prefix", "Value" },
                values: new object[] { 4, "S", 0L });

            migrationBuilder.UpdateData(
                table: "Counters",
                keyColumn: "Id",
                keyValue: 11L,
                columns: new[] { "Length", "Prefix" },
                values: new object[] { 4, "Sch" });
        }
    }
}
