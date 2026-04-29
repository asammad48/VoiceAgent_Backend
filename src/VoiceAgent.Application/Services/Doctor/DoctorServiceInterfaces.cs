namespace VoiceAgent.Application.Services.Doctor;

public interface IDoctorAvailabilityService
{
    Task<DoctorAvailabilityResult> CheckAvailabilityAsync(DoctorFlowRequest request, CancellationToken cancellationToken = default);
}

public interface IDoctorAppointmentService
{
    Task<string> BuildAppointmentResultJsonAsync(Guid callSessionId, CancellationToken cancellationToken = default);
}

public interface IEmergencyDetectionService
{
    Task<EmergencyScreeningResult> ScreenAsync(DoctorFlowRequest request, CancellationToken cancellationToken = default);
}
