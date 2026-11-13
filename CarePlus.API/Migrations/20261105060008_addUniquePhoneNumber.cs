using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class addUniquePhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE `Users`
                SET `Phone` = NULL
                WHERE `Phone` IS NOT NULL AND TRIM(`Phone`) = '';

                UPDATE `Users` AS duplicateUser
                INNER JOIN `Users` AS originalUser
                    ON duplicateUser.`Phone` = originalUser.`Phone`
                    AND duplicateUser.`UserId` > originalUser.`UserId`
                SET duplicateUser.`Phone` = NULL
                WHERE duplicateUser.`Phone` IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Phone",
                table: "Users");
        }
    }
}
