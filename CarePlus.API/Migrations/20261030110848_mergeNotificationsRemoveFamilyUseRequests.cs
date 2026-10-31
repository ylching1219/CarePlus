using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class mergeNotificationsRemoveFamilyUseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `FamilyUseRequests`;");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Notifications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Notifications",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(@"
                INSERT INTO `Notifications`
                    (`NotificationId`, `UserId`, `Title`, `Message`, `Type`, `Actor`, `Initials`, `TitleZh`, `MessageZh`, `Href`, `Status`, `IsPublished`, `CreatedAt`)
                SELECT
                    CONCAT('announcement:public:', `AnnouncementId`),
                    NULL,
                    `Title`,
                    `Body`,
                    'announcement',
                    'CarePlus Clinic',
                    'CC',
                    `TitleZh`,
                    `BodyZh`,
                    CONCAT('/announcements#announcement:public:', `AnnouncementId`),
                    'Unread',
                    `IsPublished`,
                    `PublishedAt`
                FROM `ClinicAnnouncements`;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS `ClinicAnnouncements`;");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "Type", "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.CreateTable(
                name: "ClinicAnnouncements",
                columns: table => new
                {
                    AnnouncementId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "longtext", nullable: false),
                    BodyZh = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    TitleZh = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicAnnouncements", x => x.AnnouncementId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql(@"
                INSERT INTO `ClinicAnnouncements`
                    (`AnnouncementId`, `Title`, `Body`, `TitleZh`, `BodyZh`, `PublishedAt`, `IsPublished`, `CreatedAt`)
                SELECT
                    REPLACE(`NotificationId`, 'announcement:public:', ''),
                    `Title`,
                    `Message`,
                    COALESCE(`TitleZh`, `Title`),
                    COALESCE(`MessageZh`, `Message`),
                    `CreatedAt`,
                    `IsPublished`,
                    `CreatedAt`
                FROM `Notifications`
                WHERE `UserId` IS NULL AND `Type` = 'announcement';");

            migrationBuilder.Sql("DELETE FROM `Notifications` WHERE `UserId` IS NULL AND `Type` = 'announcement';");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Notifications");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Notifications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.CreateTable(
                name: "ClinicAnnouncements",
                columns: table => new
                {
                    AnnouncementId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "longtext", nullable: false),
                    BodyZh = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    TitleZh = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicAnnouncements", x => x.AnnouncementId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FamilyUseRequests",
                columns: table => new
                {
                    FamilyUseRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FamilyGroupId = table.Column<long>(type: "bigint", nullable: false),
                    FromPatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    TargetPatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyUseRequests", x => x.FamilyUseRequestId);
                    table.ForeignKey(
                        name: "FK_FamilyUseRequests_FamilyGroups_FamilyGroupId",
                        column: x => x.FamilyGroupId,
                        principalTable: "FamilyGroups",
                        principalColumn: "FamilyGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyUseRequests_Patients_FromPatientId",
                        column: x => x.FromPatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyUseRequests_Patients_TargetPatientId",
                        column: x => x.TargetPatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAnnouncements_IsPublished_PublishedAt",
                table: "ClinicAnnouncements",
                columns: new[] { "IsPublished", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyUseRequests_FamilyGroupId_FromPatientId_TargetPatientI~",
                table: "FamilyUseRequests",
                columns: new[] { "FamilyGroupId", "FromPatientId", "TargetPatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyUseRequests_FromPatientId",
                table: "FamilyUseRequests",
                column: "FromPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyUseRequests_TargetPatientId",
                table: "FamilyUseRequests",
                column: "TargetPatientId");
        }
    }
}
