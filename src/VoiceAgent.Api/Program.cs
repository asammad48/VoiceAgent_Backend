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
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy => policy
        .WithOrigins("http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services
    .AddCommon()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiPresentation();

var app = builder.Build();

Log.Information("Starting VoiceAgent API on environment: {Environment}", app.Environment.EnvironmentName);

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
