using Serilog;
using VoiceAgent.Application;
using VoiceAgent.Common;
using VoiceAgent.Infrastructure;
using VoiceAgent.WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCommon().AddApplication().AddInfrastructure(builder.Configuration).AddWorkerServices();
builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.File("logs/voice-agent-worker-.log", rollingInterval: RollingInterval.Day).ReadFrom.Configuration(ctx.Configuration));
var app = builder.Build();
await app.RunAsync();
