using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    public partial class AddPrescriptionCounters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Counters",
                columns: new[] { "Id", "IsActive", "Key", "Length", "PaddingChar", "Prefix", "Step", "Value", "Version" },
                values: new object[,]
                {
                    { 13L, true, "PrescriptionId", 4, "0", "RX", 1, 0L, 0L },
                    { 14L, true, "PrescriptionItemId", 4, "0", "RXI", 1, 0L, 0L }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Counters",
                keyColumn: "Id",
                keyValues: new object[] { 13L, 14L });
        }
    }
}
