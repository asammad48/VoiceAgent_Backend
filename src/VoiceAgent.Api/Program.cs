using Microsoft.EntityFrameworkCore;
using Serilog;
using VoiceAgent.Api;
using VoiceAgent.Application;
using VoiceAgent.Common;
using VoiceAgent.Infrastructure;
using VoiceAgent.Infrastructure.Persistence;
using VoiceAgent.Infrastructure.Persistence.Seed;
using VoiceAgent.Application.Interfaces.Voice;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.DocumentFilter<WebSocketDocumentFilter>();
});
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// Bind TtsLocale before AddApplication so IOptions<TtsLocale> resolves from config.
builder.Services.Configure<VoiceAgent.Application.Services.Voice.TtsLocale>(
    builder.Configuration.GetSection("TtsLocale"));

builder.Services
    .AddCommon()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiPresentation();

var app = builder.Build();

Log.Information("Starting VoiceAgent API on environment: {Environment}", app.Environment.EnvironmentName);

// Confirm DI logging pipeline is wired: if this line appears in the log file,
// every ILogger<T>-injected service will also write to the same sinks.
app.Logger.LogInformation("[Startup] DI ILogger pipeline active — all ILogger<T> services are wired to Serilog.");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Log.Information("Applying database migrations...");
        db.Database.Migrate();
        Log.Information("Database migration complete.");

        if (app.Environment.IsDevelopment())
        {
            Log.Information("Seeding demo data...");
            await DatabaseSeeder.SeedAsync(db);
            Log.Information("Database seed complete.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration/seed failed during startup.");
    }
}

// Logs every HTTP request/response (method, path, status, duration).
// Source context is Serilog.AspNetCore — unaffected by the Microsoft/System overrides.
app.UseSerilogRequestLogging();

app.UseCors("LocalFrontend");
app.UseWebSockets();
app.MapHealthChecks("/health");
app.MapControllers();

app.Map("/api/voice/web-stream", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    using var scope = context.RequestServices.CreateScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<IVoiceStreamOrchestrator>();
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await orchestrator.HandleWebSocketAsync(socket, "web", context.RequestAborted);
});

app.Map("/api/voice/phone-stream", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    using var scope = context.RequestServices.CreateScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<IVoiceStreamOrchestrator>();
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await orchestrator.HandleWebSocketAsync(socket, "phone", context.RequestAborted);
});


Log.Information("VoiceAgent API startup complete.");
app.Run();
