using Microsoft.Extensions.DependencyInjection;
using VoiceAgent.Application.Interfaces;
using VoiceAgent.Application.Interfaces.Tools;
using VoiceAgent.Application.Services.Tools;
using VoiceAgent.Application.Services;
using VoiceAgent.Application.Interfaces.Voice;
using VoiceAgent.Application.Services.Voice;

namespace VoiceAgent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddScoped<IDemoConversationService, DemoConversationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConversationOrchestratorService, ConversationOrchestratorService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ICampaignConfigurationService, CampaignConfigurationService>();
        services.AddScoped<ICallQueryService, CallQueryService>();
        services.AddScoped<IRestaurantMenuService, RestaurantMenuService>();
        services.AddScoped<IRestaurantDealService, RestaurantDealService>();
        services.AddScoped<ICourierPricingService, CourierPricingService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IExternalApiConfigurationService, ExternalApiConfigurationService>();
        services.AddScoped<IProviderCostService, ProviderCostService>();
        services.AddScoped<IContactUsService, ContactUsService>();
        services.AddScoped<IOutboundService, OutboundService>();
        services.AddScoped<IRecordingService, RecordingService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<VoiceAgent.Application.Services.Rag.IRagRetrievalService, VoiceAgent.Application.Services.Rag.DbRagRetrievalService>();
        services.AddScoped<IToolExecutionService, ToolExecutionService>();
        services.AddScoped<IVoiceSessionService, VoiceSessionService>();
        services.AddScoped<IVoiceStreamOrchestrator, VoiceStreamOrchestrator>();
        services.AddScoped<IAudioStreamRouter, AudioStreamRouter>();
        services.AddScoped<ISpeechEndDetectionService, SpeechEndDetectionService>();
        services.AddScoped<IBargeInService, BargeInService>();
        services.AddScoped<ICallRecordingService, CallRecordingService>();
        services.AddScoped<ICallCostTrackingService, CallCostTrackingService>();
        return services;
    }
}
