using CarePlus.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <summary>
    /// Adds short-lived development timetable data for the existing doctors.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027150000_seedBookingTestSchedules")]
    public partial class seedBookingTestSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO `WorkingSchedules`
                    (`ScheduleId`, `StaffId`, `WorkingDate`, `StartTime`, `EndTime`, `Status`)
                SELECT
                    CONCAT(d.`StaffId`, '-TEST-AM-', DATE_FORMAT(DATE_ADD(CURDATE(), INTERVAL days.`DayOffset` DAY), '%Y%m%d')),
                    d.`StaffId`,
                    DATE_ADD(CURDATE(), INTERVAL days.`DayOffset` DAY),
                    '09:00:00',
                    '17:00:00',
                    'Active'
                FROM `Doctors` d
                CROSS JOIN (
                    SELECT 1 AS `DayOffset`
                    UNION ALL SELECT 2
                    UNION ALL SELECT 3
                    UNION ALL SELECT 4
                    UNION ALL SELECT 5
                    UNION ALL SELECT 6
                    UNION ALL SELECT 7
                ) days
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM `WorkingSchedules` existing
                    WHERE existing.`StaffId` = d.`StaffId`
                      AND existing.`WorkingDate` = DATE_ADD(CURDATE(), INTERVAL days.`DayOffset` DAY)
                      AND existing.`Status` = 'Active'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM `WorkingSchedules`
                WHERE `ScheduleId` LIKE '%-TEST-AM-%';
                """);
        }
    }
}
