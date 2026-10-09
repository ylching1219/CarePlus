using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class addFamilyGroup_Member : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamilyGroups",
                columns: table => new
                {
                    FamilyGroupId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GroupName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(255)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyGroups", x => x.FamilyGroupId);
                    table.ForeignKey(
                        name: "FK_FamilyGroups_Patients_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    FamilyMemberId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FamilyGroupId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Relationship = table.Column<string>(type: "longtext", nullable: false),
                    AccessLevel = table.Column<string>(type: "longtext", nullable: false),
                    JoinAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.FamilyMemberId);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_FamilyGroups_FamilyGroupId",
                        column: x => x.FamilyGroupId,
                        principalTable: "FamilyGroups",
                        principalColumn: "FamilyGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyGroups_CreatedBy",
                table: "FamilyGroups",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_FamilyGroupId",
                table: "FamilyMembers",
                column: "FamilyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_PatientId",
                table: "FamilyMembers",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "FamilyGroups");
        }
    }
}
