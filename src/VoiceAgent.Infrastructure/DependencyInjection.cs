using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Application.Interfaces.Providers;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Infrastructure.Caching;
using VoiceAgent.Infrastructure.Http;
using VoiceAgent.Infrastructure.Persistence;
using VoiceAgent.Infrastructure.Persistence.Seed;
using VoiceAgent.Infrastructure.Providers;
using VoiceAgent.Infrastructure.Providers.Llm;
using VoiceAgent.Infrastructure.Providers.Maps;
using VoiceAgent.Infrastructure.Providers.Speech;
using VoiceAgent.Infrastructure.Providers.Storage;
using VoiceAgent.Infrastructure.Providers.Telephony;
using VoiceAgent.Infrastructure.Providers.Voice;
using VoiceAgent.Infrastructure.Providers.IntentDetection;
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

        // Retry handler — transient; one instance per HTTP request pipeline
        services.AddTransient<HttpRetryHandler>();

        services.AddHttpClient<GeminiClient>().AddHttpMessageHandler<HttpRetryHandler>();
        services.AddHttpClient<DeepgramClient>().AddHttpMessageHandler<HttpRetryHandler>();
        services.AddHttpClient<ElevenLabsClient>().AddHttpMessageHandler<HttpRetryHandler>();
        services.AddHttpClient<NominatimGeocodingClient>().AddHttpMessageHandler<HttpRetryHandler>();
        services.AddHttpClient<OsrmRoutingClient>().AddHttpMessageHandler<HttpRetryHandler>();
        services.AddHttpClient<CloudflareR2StorageClient>().AddHttpMessageHandler<HttpRetryHandler>();
        services.AddHttpClient<TelnyxTelephonyProvider>().AddHttpMessageHandler<HttpRetryHandler>();
        services.AddSingleton<FreeSwitchTelephonyProvider>();
        services.AddScoped<IStreamingSpeechToTextProvider, DeepgramStreamingSpeechToTextProvider>();
        services.AddScoped<IStreamingTextToSpeechProvider, ElevenLabsStreamingTextToSpeechProvider>();
        services.AddScoped<FreeSwitchAudioBridge>();
        services.AddScoped<TelnyxWebhookHandler>();
        services.AddScoped<CloudflareR2RecordingStorage>();

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

        if (useMockProviders)
        {
            services.AddScoped<ILookupService, MockLookupService>();
            services.AddScoped<ISlotExtractionService, MockSlotExtractionService>();
            services.AddScoped<IAnswerFinalizationService, MockAnswerFinalizationService>();
            services.AddScoped<ILocationNormalizationService, MockLocationNormalizationService>();
            services.AddScoped<IIntentDetectionService, MockIntentDetectionService>();
        }
        else
        {
            services.AddHttpClient("ExternalLookup").AddHttpMessageHandler<HttpRetryHandler>();
            services.AddScoped<ILookupService, ProductionLookupService>();
            services.AddScoped<ISlotExtractionService, GeminiSlotExtractionService>();
            services.AddScoped<IAnswerFinalizationService, GeminiAnswerFinalizationService>();
            services.AddScoped<ILocationNormalizationService, GeminiLocationNormalizationService>();
            services.AddScoped<IIntentDetectionService, GeminiIntentDetectionService>();
        }

        services.AddScoped<DbSeeder>();
        return services;
    }
}
