namespace VoiceAgent.Application.Interfaces;
public interface IBranchService { Task<Guid> CreateAsync(object request, CancellationToken ct=default); }
