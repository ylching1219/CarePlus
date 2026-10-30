using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class familyApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamilyUseRequests",
                columns: table => new
                {
                    FamilyUseRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FamilyGroupId = table.Column<long>(type: "bigint", nullable: false),
                    FromPatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    TargetPatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                name: "IX_FamilyMembers_FamilyGroupId_PatientId",
                table: "FamilyMembers",
                columns: new[] { "FamilyGroupId", "PatientId" },
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyUseRequests");

            migrationBuilder.DropIndex(
                name: "IX_FamilyMembers_FamilyGroupId_PatientId",
                table: "FamilyMembers");

            // Keep the original FamilyGroupId index because MySQL requires it for the foreign key.
        }
    }
}
