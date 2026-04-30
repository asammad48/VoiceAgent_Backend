using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoiceAgent.Application.Abstractions;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Infrastructure.Caching;
using VoiceAgent.Infrastructure.Persistence;
using VoiceAgent.Infrastructure.Persistence.Seed;
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

        services.Configure<GeminiOptions>(configuration.GetSection("Providers:Gemini"));
        services.Configure<DeepgramOptions>(configuration.GetSection("Providers:Deepgram"));
        services.Configure<ElevenLabsOptions>(configuration.GetSection("Providers:ElevenLabs"));
        services.Configure<NominatimOptions>(configuration.GetSection("Providers:Nominatim"));
        services.Configure<OsrmOptions>(configuration.GetSection("Providers:Osrm"));
        services.Configure<CloudflareR2Options>(configuration.GetSection("Providers:CloudflareR2"));
        services.Configure<FreeSwitchOptions>(configuration.GetSection("Providers:FreeSwitch"));
        services.Configure<TelnyxOptions>(configuration.GetSection("Providers:Telnyx"));

        services.AddHttpClient<GeminiClient>();
        services.AddHttpClient<DeepgramClient>();
        services.AddHttpClient<ElevenLabsClient>();
        services.AddHttpClient<NominatimGeocodingClient>();
        services.AddHttpClient<OsrmRoutingClient>();
        services.AddHttpClient<CloudflareR2StorageClient>();
        services.AddHttpClient<TelnyxTelephonyProvider>();
        services.AddSingleton<FreeSwitchTelephonyProvider>();

        services.AddScoped<IAgentTool, MenuCategorySearchTool>();
        services.AddScoped<IAgentTool, MenuItemSearchTool>();
        services.AddScoped<IAgentTool, ListDealsTool>();
        services.AddScoped<IAgentTool, RestaurantTotalTool>();
        services.AddScoped<IAgentTool, CourierQuoteTool>();

        services.AddScoped<DbSeeder>();
        return services;
    }
}
