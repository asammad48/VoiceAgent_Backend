using VoiceAgent.Application.Interfaces;

namespace VoiceAgent.Infrastructure.Providers;

public sealed class MockAnswerFinalizationService : IAnswerFinalizationService
{
    public Task<AnswerFinalizationResult> FinalizeAnswersAsync(
        IReadOnlyList<SlotAnswer> answers,
        CancellationToken ct = default)
        => Task.FromResult(new AnswerFinalizationResult(true, []));
}
