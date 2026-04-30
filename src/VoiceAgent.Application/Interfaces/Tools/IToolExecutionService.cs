namespace VoiceAgent.Application.Interfaces.Tools;
public interface IToolExecutionService { Task<string> ExecuteAsync(string toolName, string input, CancellationToken ct=default); }
