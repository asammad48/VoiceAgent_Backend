using System.Collections.Concurrent;

namespace VoiceAgent.Infrastructure.Caching;

public sealed class InMemoryConversationStateStore
{
    private readonly ConcurrentDictionary<Guid, string> _state = new();
    public void Set(Guid sessionId, string state) => _state[sessionId] = state;
    public bool TryGet(Guid sessionId, out string? state) => _state.TryGetValue(sessionId, out state);
    public void Remove(Guid sessionId) => _state.TryRemove(sessionId, out _);
}
