using Serilog;
using VoiceAgent.Api;
using VoiceAgent.Application;
using VoiceAgent.Common;
using VoiceAgent.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.File(ctx.Configuration["Serilog:FilePath"] ?? "logs/voice-agent-api-.log", rollingInterval: RollingInterval.Day)
    .ReadFrom.Configuration(ctx.Configuration));

builder.Services
    .AddCommon()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiPresentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
