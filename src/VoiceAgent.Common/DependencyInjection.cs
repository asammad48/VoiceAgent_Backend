using Microsoft.Extensions.DependencyInjection;
using VoiceAgent.Common.Providers;

namespace VoiceAgent.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
        return services;
    }
}
