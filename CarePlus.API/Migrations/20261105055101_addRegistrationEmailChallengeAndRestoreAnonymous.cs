using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class addRegistrationEmailChallengeAndRestoreAnonymous : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "DoctorReviews",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RegistrationEmailChallenges",
                columns: table => new
                {
                    ChallengeId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false),
                    SecretHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationEmailChallenges", x => x.ChallengeId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEmailChallenges_Email_CreatedAt",
                table: "RegistrationEmailChallenges",
                columns: new[] { "Email", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationEmailChallenges");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "DoctorReviews");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)");
        }
    }
}
