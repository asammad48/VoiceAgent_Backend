namespace VoiceAgent.Application.Interfaces;
public interface IProviderCostService { Task<decimal> EstimateAsync(Guid callSessionId, CancellationToken ct=default); }
