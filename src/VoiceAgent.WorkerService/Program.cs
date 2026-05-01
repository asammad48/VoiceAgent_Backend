using Serilog;
using VoiceAgent.Application;
using VoiceAgent.Common;
using VoiceAgent.Infrastructure;
using VoiceAgent.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services
    .AddCommon()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddWorkerServices();

var app = builder.Build();
await app.RunAsync();
