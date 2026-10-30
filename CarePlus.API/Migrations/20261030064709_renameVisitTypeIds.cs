using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class renameVisitTypeIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE `VisitTypes` SET `VisitTypeId` = 'VT01' WHERE `VisitTypeId` = 'general';");
            migrationBuilder.Sql("UPDATE `VisitTypes` SET `VisitTypeId` = 'VT02' WHERE `VisitTypeId` = 'screening';");
            migrationBuilder.Sql("UPDATE `VisitTypes` SET `VisitTypeId` = 'VT03' WHERE `VisitTypeId` = 'mental';");
            migrationBuilder.Sql("UPDATE `Appointments` SET `VisitType` = 'VT01' WHERE `VisitType` = 'general';");
            migrationBuilder.Sql("UPDATE `Appointments` SET `VisitType` = 'VT02' WHERE `VisitType` = 'screening';");
            migrationBuilder.Sql("UPDATE `Appointments` SET `VisitType` = 'VT03' WHERE `VisitType` = 'mental';");
            migrationBuilder.Sql("UPDATE `Invoices` SET `VisitType` = 'VT01' WHERE `VisitType` = 'general';");
            migrationBuilder.Sql("UPDATE `Invoices` SET `VisitType` = 'VT02' WHERE `VisitType` = 'screening';");
            migrationBuilder.Sql("UPDATE `Invoices` SET `VisitType` = 'VT03' WHERE `VisitType` = 'mental';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE `VisitTypes` SET `VisitTypeId` = 'general' WHERE `VisitTypeId` = 'VT01';");
            migrationBuilder.Sql("UPDATE `VisitTypes` SET `VisitTypeId` = 'screening' WHERE `VisitTypeId` = 'VT02';");
            migrationBuilder.Sql("UPDATE `VisitTypes` SET `VisitTypeId` = 'mental' WHERE `VisitTypeId` = 'VT03';");
            migrationBuilder.Sql("UPDATE `Appointments` SET `VisitType` = 'general' WHERE `VisitType` = 'VT01';");
            migrationBuilder.Sql("UPDATE `Appointments` SET `VisitType` = 'screening' WHERE `VisitType` = 'VT02';");
            migrationBuilder.Sql("UPDATE `Appointments` SET `VisitType` = 'mental' WHERE `VisitType` = 'VT03';");
            migrationBuilder.Sql("UPDATE `Invoices` SET `VisitType` = 'general' WHERE `VisitType` = 'VT01';");
            migrationBuilder.Sql("UPDATE `Invoices` SET `VisitType` = 'screening' WHERE `VisitType` = 'VT02';");
            migrationBuilder.Sql("UPDATE `Invoices` SET `VisitType` = 'mental' WHERE `VisitType` = 'VT03';");
        }
    }
}
