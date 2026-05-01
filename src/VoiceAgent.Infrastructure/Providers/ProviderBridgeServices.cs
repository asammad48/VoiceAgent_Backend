using VoiceAgent.Application.Interfaces.Providers;
using VoiceAgent.Infrastructure.Providers.Llm;
using VoiceAgent.Infrastructure.Providers.Maps;
using VoiceAgent.Infrastructure.Providers.Speech;
using VoiceAgent.Infrastructure.Providers.Storage;
using VoiceAgent.Infrastructure.Providers.Telephony;
using VoiceAgent.Infrastructure.Providers.Voice;

namespace VoiceAgent.Infrastructure.Providers;

internal sealed class LlmProviderBridge(GeminiClient client) : ILlmProvider;
internal sealed class SpeechToTextProviderBridge(DeepgramClient client) : ISpeechToTextProvider;
internal sealed class TextToSpeechProviderBridge(ElevenLabsClient client) : ITextToSpeechProvider;
internal sealed class GeocodingProviderBridge(NominatimGeocodingClient client) : IGeocodingProvider
{
    public Task<(double Latitude, double Longitude)?> GeocodeAsync(string address, CancellationToken ct = default)
        => client.GeocodeAsync(address, ct);
}

internal sealed class RoutingProviderBridge(OsrmRoutingClient client) : IRoutingProvider
{
    public Task<decimal?> GetDistanceKmAsync((double Latitude, double Longitude) from, (double Latitude, double Longitude) to, CancellationToken ct = default)
        => client.GetDistanceKmAsync(from, to, ct);
}
internal sealed class ObjectStorageProviderBridge(CloudflareR2StorageClient client) : IObjectStorageProvider;
internal sealed class TelephonyProviderBridge(FreeSwitchTelephonyProvider freeSwitch, TelnyxTelephonyProvider telnyx) : ITelephonyProvider;
