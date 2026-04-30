using Microsoft.Extensions.DependencyInjection;
using VoiceAgent.WorkerService.Workers;

namespace VoiceAgent.WorkerService;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddHostedService<PlaceholderWorker>();
        return services;
    }
}
