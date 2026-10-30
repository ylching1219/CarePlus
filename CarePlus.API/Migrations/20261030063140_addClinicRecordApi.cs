using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class addClinicRecordApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "VisitTypes",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "VisitTypes",
                keyColumn: "VisitTypeId",
                keyValue: "general",
                column: "Price",
                value: 50m);

            migrationBuilder.UpdateData(
                table: "VisitTypes",
                keyColumn: "VisitTypeId",
                keyValue: "mental",
                column: "Price",
                value: 60m);

            migrationBuilder.UpdateData(
                table: "VisitTypes",
                keyColumn: "VisitTypeId",
                keyValue: "screening",
                column: "Price",
                value: 55m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "VisitTypes");
        }
    }
}
