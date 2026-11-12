using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace CarePlus.API.Migrations
{
    /// <inheritdoc />
    public partial class addChatHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatThreads",
                columns: table => new
                {
                    ChatThreadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PatientId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatThreads", x => x.ChatThreadId);
                    table.ForeignKey(
                        name: "FK_ChatThreads_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    ChatMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ChatThreadId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    Disclaimer = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    SourcesJson = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.ChatMessageId);
                    table.CheckConstraint("CK_ChatMessages_Role", "`Role` IN ('user', 'assistant')");
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatThreads_ChatThreadId",
                        column: x => x.ChatThreadId,
                        principalTable: "ChatThreads",
                        principalColumn: "ChatThreadId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatThreadId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "ChatThreadId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatThreads_PatientId_UpdatedAt",
                table: "ChatThreads",
                columns: new[] { "PatientId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatThreads");

        }
    }
}
