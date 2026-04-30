using Microsoft.Extensions.DependencyInjection;

namespace VoiceAgent.WorkerService;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        return services;
    }
}
