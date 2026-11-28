using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TornDiscordBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    DiscordUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    SentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    NextCheckAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => new { x.DiscordUserId, x.Type });
                    table.ForeignKey(
                        name: "FK_Reminders_RegisteredUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "RegisteredUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reminders");
        }
    }
}
