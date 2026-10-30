using CarePlus.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <summary>
    /// Adds a development-only doctor so the patient booking flow has data to retrieve.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027160000_seedBookingTestDoctor")]
    public partial class seedBookingTestDoctor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO `Users`
                    (`Username`, `Name`, `Email`, `PasswordHash`, `Phone`, `Status`, `CreateDate`)
                SELECT
                    'booking.test.doctor',
                    'Dr Booking Test',
                    'booking.test.doctor@careplus.local',
                    'AQEAAACghgEAEAAAACX0bfzG+aPxLqKTMRI/MIWeJt1U997tyfFEyvF7TrdE7LdyJT5Fuiz+g+U0+HcgYA==',
                    NULL,
                    'Active',
                    NOW()
                WHERE NOT EXISTS (SELECT 1 FROM `Doctors`);
                """);

            migrationBuilder.Sql("""
                INSERT INTO `Staffs` (`StaffId`, `UserId`, `Role`)
                SELECT 'TESTDOC1', `UserId`, 'Doctor'
                FROM `Users`
                WHERE `Username` = 'booking.test.doctor'
                  AND NOT EXISTS (SELECT 1 FROM `Staffs` WHERE `StaffId` = 'TESTDOC1');
                """);

            migrationBuilder.Sql("""
                INSERT INTO `Doctors`
                    (`StaffId`, `LicenseNumber`, `Specialization`, `Qualification`, `YearsOfExperience`)
                SELECT
                    'TESTDOC1',
                    'TEST-LIC-0001',
                    'General Medicine',
                    'MBBS (Test Data)',
                    5
                WHERE EXISTS (SELECT 1 FROM `Staffs` WHERE `StaffId` = 'TESTDOC1')
                  AND NOT EXISTS (SELECT 1 FROM `Doctors` WHERE `StaffId` = 'TESTDOC1');
                """);

            migrationBuilder.Sql("""
                INSERT INTO `appSetting` (`UserId`, `Appearance`, `Language`, `Notification`)
                SELECT `UserId`, 'light', 'en', 'all'
                FROM `Users`
                WHERE `Username` = 'booking.test.doctor'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM `appSetting` existing
                      WHERE existing.`UserId` = `Users`.`UserId`
                  );
                """);

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

            migrationBuilder.Sql("""
                DELETE FROM `Doctors`
                WHERE `StaffId` = 'TESTDOC1';
                """);

            migrationBuilder.Sql("""
                DELETE FROM `Staffs`
                WHERE `StaffId` = 'TESTDOC1';
                """);

            migrationBuilder.Sql("""
                DELETE FROM `appSetting`
                WHERE `UserId` = (
                    SELECT `UserId`
                    FROM `Users`
                    WHERE `Username` = 'booking.test.doctor'
                    LIMIT 1
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM `Users`
                WHERE `Username` = 'booking.test.doctor';
                """);
        }
    }
}
