using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectedIntentToCallSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetectedIntent",
                schema: "public",
                table: "CallSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetectedIntent",
                schema: "public",
                table: "CallSessions");
        }
    }
}
