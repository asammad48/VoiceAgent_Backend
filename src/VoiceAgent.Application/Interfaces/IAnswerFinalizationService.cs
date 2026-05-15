namespace VoiceAgent.Application.Interfaces;

public sealed record SlotAnswer(string SlotId, string Question, string Answer, string? SlotType);

public sealed record AnswerFinalizationResult(bool AllClear, IReadOnlyList<string> AmbiguousSlotIds);

public interface IAnswerFinalizationService
{
    Task<AnswerFinalizationResult> FinalizeAnswersAsync(
        IReadOnlyList<SlotAnswer> answers,
        CancellationToken ct = default);
}
