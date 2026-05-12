namespace VoiceAgent.Application.Interfaces;

public interface ISlotExtractionService
{
    Task<string?> ExtractAsync(string slotId, string question, string userMessage, CancellationToken ct = default);
}
