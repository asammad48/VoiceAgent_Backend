namespace VoiceAgent.Application.Interfaces;

public interface ISlotExtractionService
{
    /// <param name="slotType">Optional type hint ("text"|"number"|"date"|"datetime"|"phone"|"yesno"|"enum") to constrain extraction.</param>
    Task<string?> ExtractAsync(string slotId, string question, string userMessage, string? slotType, CancellationToken ct = default);
}
