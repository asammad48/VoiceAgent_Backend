using Microsoft.Extensions.DependencyInjection;
namespace VoiceAgent.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();
        return services;
    }
}
