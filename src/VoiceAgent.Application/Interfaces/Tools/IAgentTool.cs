namespace VoiceAgent.Application.Interfaces.Tools;
public interface IAgentTool { string Name { get; } Task<string> ExecuteAsync(string input, CancellationToken ct=default); }
