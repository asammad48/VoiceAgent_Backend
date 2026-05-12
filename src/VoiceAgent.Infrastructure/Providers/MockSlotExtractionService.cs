using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Infrastructure.Providers;

public class MockSlotExtractionService : ISlotExtractionService
{
    public Task<string?> ExtractAsync(string slotId, string question, string userMessage, CancellationToken ct = default)
        => Task.FromResult<string?>(null);
}
