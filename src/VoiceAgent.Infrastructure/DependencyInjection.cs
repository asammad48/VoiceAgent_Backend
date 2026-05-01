using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Providers;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Infrastructure.Caching;
using VoiceAgent.Infrastructure.Persistence;
using VoiceAgent.Infrastructure.Persistence.Seed;
using VoiceAgent.Infrastructure.Providers;
using VoiceAgent.Infrastructure.Providers.Llm;
using VoiceAgent.Infrastructure.Providers.Maps;
using VoiceAgent.Infrastructure.Providers.Speech;
using VoiceAgent.Infrastructure.Providers.Storage;
using VoiceAgent.Infrastructure.Providers.Telephony;
using VoiceAgent.Infrastructure.Providers.Voice;
using VoiceAgent.Infrastructure.Tools.Courier;
using VoiceAgent.Infrastructure.Tools.Restaurant;

namespace VoiceAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<InMemoryConversationStateStore>();

        services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        var useMockProviders = configuration.GetValue<bool>("FeatureFlags:UseMockProviders", true);

        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.Configure<DeepgramOptions>(configuration.GetSection("Deepgram"));
        services.Configure<ElevenLabsOptions>(configuration.GetSection("ElevenLabs"));
        services.Configure<NominatimOptions>(configuration.GetSection("Nominatim"));
        services.Configure<OsrmOptions>(configuration.GetSection("Osrm"));
        services.Configure<CloudflareR2Options>(configuration.GetSection("CloudflareR2"));
        services.Configure<FreeSwitchOptions>(configuration.GetSection("FreeSwitch"));
        services.Configure<TelnyxOptions>(configuration.GetSection("Telnyx"));

        services.PostConfigure<GeminiOptions>(o => o.UseMockProviders = useMockProviders);
        services.PostConfigure<DeepgramOptions>(o => o.UseMockProviders = useMockProviders);
        services.PostConfigure<ElevenLabsOptions>(o => o.UseMockProviders = useMockProviders);
        services.PostConfigure<NominatimOptions>(o => o.UseMockProviders = useMockProviders);
        services.PostConfigure<OsrmOptions>(o => o.UseMockProviders = useMockProviders);
        services.PostConfigure<CloudflareR2Options>(o => o.UseMockProviders = useMockProviders);
        services.PostConfigure<FreeSwitchOptions>(o => o.UseMockProviders = useMockProviders);
        services.PostConfigure<TelnyxOptions>(o => o.UseMockProviders = useMockProviders);

        services.AddHttpClient<GeminiClient>();
        services.AddHttpClient<DeepgramClient>();
        services.AddHttpClient<ElevenLabsClient>();
        services.AddHttpClient<NominatimGeocodingClient>();
        services.AddHttpClient<OsrmRoutingClient>();
        services.AddHttpClient<CloudflareR2StorageClient>();
        services.AddHttpClient<TelnyxTelephonyProvider>();
        services.AddSingleton<FreeSwitchTelephonyProvider>();

        services.AddScoped<ILlmProvider, LlmProviderBridge>();
        services.AddScoped<ISpeechToTextProvider, SpeechToTextProviderBridge>();
        services.AddScoped<ITextToSpeechProvider, TextToSpeechProviderBridge>();
        services.AddScoped<IGeocodingProvider, GeocodingProviderBridge>();
        services.AddScoped<IRoutingProvider, RoutingProviderBridge>();
        services.AddScoped<IObjectStorageProvider, ObjectStorageProviderBridge>();
        services.AddScoped<ITelephonyProvider, TelephonyProviderBridge>();

        services.AddScoped<IAgentTool, MenuCategorySearchTool>();
        services.AddScoped<IAgentTool, MenuItemSearchTool>();
        services.AddScoped<IAgentTool, ListDealsTool>();
        services.AddScoped<IAgentTool, RestaurantTotalTool>();
        services.AddScoped<IAgentTool, CourierQuoteTool>();

        services.AddScoped<DbSeeder>();
        return services;
    }
}
