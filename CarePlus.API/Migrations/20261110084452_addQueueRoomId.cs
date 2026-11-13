using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class addQueueRoomId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoomId",
                table: "QueueEntries",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_RoomId",
                table: "QueueEntries",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_ClinicRooms_RoomId",
                table: "QueueEntries",
                column: "RoomId",
                principalTable: "ClinicRooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_ClinicRooms_RoomId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_RoomId",
                table: "QueueEntries");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "QueueEntries");
        }
    }
}
