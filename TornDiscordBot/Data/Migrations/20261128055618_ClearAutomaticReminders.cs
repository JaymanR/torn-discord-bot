using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TornDiscordBot.Data.Migrations
{
    /// <summary>
    /// Deletes the reminders that were created automatically for every registered user, as reminders are now opt-in.
    /// </summary>
    public partial class ClearAutomaticReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Reminders;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The deleted rows only held progress, so there is nothing to restore.
        }
    }
}
