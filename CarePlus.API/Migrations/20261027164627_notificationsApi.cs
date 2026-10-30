using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class notificationsApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Actor",
                table: "Notifications",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "CarePlus Clinic");

            migrationBuilder.AddColumn<string>(
                name: "Href",
                table: "Notifications",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "/notifications");

            migrationBuilder.AddColumn<string>(
                name: "Initials",
                table: "Notifications",
                type: "varchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "CC");

            migrationBuilder.AddColumn<string>(
                name: "MessageZh",
                table: "Notifications",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleZh",
                table: "Notifications",
                type: "longtext",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicAnnouncements",
                columns: table => new
                {
                    AnnouncementId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    Body = table.Column<string>(type: "longtext", nullable: false),
                    TitleZh = table.Column<string>(type: "longtext", nullable: false),
                    BodyZh = table.Column<string>(type: "longtext", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicAnnouncements", x => x.AnnouncementId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAnnouncements_IsPublished_PublishedAt",
                table: "ClinicAnnouncements",
                columns: new[] { "IsPublished", "PublishedAt" });

            migrationBuilder.InsertData(
                table: "ClinicAnnouncements",
                columns: new[] { "AnnouncementId", "Title", "Body", "TitleZh", "BodyZh", "PublishedAt", "IsPublished", "CreatedAt" },
                values: new object[,]
                {
                    { "flu-2026-10", "Flu vaccination walk-in this week", "Reception is taking flu shot walk-ins from 18 to 24 October, 9:00 am to 5:00 pm. Bring your identity card. No appointment is needed.", "本周可直接接种流感疫苗", "10 月 18 日至 24 日，上午 9 点至下午 5 点，可在前台直接接种流感疫苗。请携带身份证，不需要预约。", new DateTime(2026, 10, 18, 8, 0, 0), true, new DateTime(2026, 10, 18, 8, 0, 0) },
                    { "screening-2026-10", "Saturday morning screening", "Dengue and blood pressure screening is open this Saturday, 8:00 am to 12:00 pm, at the waiting lounge. The screening is free for CarePlus patients and guests.", "周六上午体检", "本周六上午 8 点至中午 12 点，候诊区提供登革热和血压检查。CarePlus 病人和访客都可以免费参加。", new DateTime(2026, 10, 12, 8, 0, 0), true, new DateTime(2026, 10, 12, 8, 0, 0) },
                    { "hours-2026-10", "Night clinic and pharmacy pickup", "The clinic stays open overnight. Pharmacy pickup for ready prescriptions closes at 10:00 pm. After that, collect the medicine the next morning.", "夜间门诊与取药时间", "诊所通宵开放。已备好的处方可在晚上 10 点前到药房领取，过了这个时间请于次日早上领取。", new DateTime(2026, 10, 2, 8, 0, 0), true, new DateTime(2026, 10, 2, 8, 0, 0) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicAnnouncements");

            migrationBuilder.DropColumn(
                name: "Actor",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Href",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Initials",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "MessageZh",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TitleZh",
                table: "Notifications");

        }
    }
}
