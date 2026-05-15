using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCabBookingAndDoctorAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CabBookings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    PickupLocation = table.Column<string>(type: "text", nullable: false),
                    DropoffLocation = table.Column<string>(type: "text", nullable: false),
                    PickupDateTime = table.Column<string>(type: "text", nullable: false),
                    PassengerCount = table.Column<int>(type: "integer", nullable: false),
                    VehicleType = table.Column<string>(type: "text", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedFare = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsAirportPickup = table.Column<bool>(type: "boolean", nullable: false),
                    IsNightSurcharge = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CabBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorAppointments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CallSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    ReasonForVisit = table.Column<string>(type: "text", nullable: false),
                    PreferredDateTime = table.Column<string>(type: "text", nullable: false),
                    PreferredDoctor = table.Column<string>(type: "text", nullable: false),
                    ClinicBranch = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAppointments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CabBookings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DoctorAppointments",
                schema: "public");
        }
    }
}
