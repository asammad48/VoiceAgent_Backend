using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEditingSlotId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbuseWarningCount",
                schema: "public",
                table: "CallSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrentQuestionId",
                schema: "public",
                table: "CallSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditingSlotId",
                schema: "public",
                table: "CallSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                schema: "public",
                table: "CallSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbuseWarningCount",
                schema: "public",
                table: "CallSessions");

            migrationBuilder.DropColumn(
                name: "CurrentQuestionId",
                schema: "public",
                table: "CallSessions");

            migrationBuilder.DropColumn(
                name: "EditingSlotId",
                schema: "public",
                table: "CallSessions");

            migrationBuilder.DropColumn(
                name: "EndReason",
                schema: "public",
                table: "CallSessions");
        }
    }
}
