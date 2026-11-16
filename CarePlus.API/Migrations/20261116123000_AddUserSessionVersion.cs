using CarePlus.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261116123000_AddUserSessionVersion")]
    public partial class AddUserSessionVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists = (
                    SELECT COUNT(*) FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Users'
                      AND COLUMN_NAME = 'SessionVersion');
                SET @ddl = IF(@col_exists = 0,
                    'ALTER TABLE `Users` ADD COLUMN `SessionVersion` int NOT NULL DEFAULT 0',
                    'SELECT 1');
                PREPARE session_version_stmt FROM @ddl;
                EXECUTE session_version_stmt;
                DEALLOCATE PREPARE session_version_stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "Users");
        }
    }
}
