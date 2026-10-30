using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class dltTreatmnet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Treatment",
                table: "MedicalRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Treatment",
                table: "MedicalRecords",
                type: "longtext",
                nullable: false);
        }
    }
}
