using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveStaffRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StaffUserId",
                table: "LeaveApplications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApplications_StaffUserId",
                table: "LeaveApplications",
                column: "StaffUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveApplications_Staffs_StaffUserId",
                table: "LeaveApplications",
                column: "StaffUserId",
                principalTable: "Staffs",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveApplications_Staffs_StaffUserId",
                table: "LeaveApplications");

            migrationBuilder.DropIndex(
                name: "IX_LeaveApplications_StaffUserId",
                table: "LeaveApplications");

            migrationBuilder.DropColumn(
                name: "StaffUserId",
                table: "LeaveApplications");
        }
    }
}
