using Microsoft.EntityFrameworkCore;
using Serilog;
using VoiceAgent.Api;
using VoiceAgent.Application;
using VoiceAgent.Common;
using VoiceAgent.Infrastructure;
using VoiceAgent.Infrastructure.Persistence;
using VoiceAgent.Infrastructure.Persistence.Seed;

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

    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Log.Information("Applying database migrations...");
        db.Database.Migrate();
        Log.Information("Database migration complete. Seeding demo data...");
        await DatabaseSeeder.SeedAsync(db);
        Log.Information("Database seed complete.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration/seed failed during startup.");
    }
}

app.UseCors("LocalFrontend");
app.MapHealthChecks("/health");
app.MapControllers();

Log.Information("VoiceAgent API startup complete.");
app.Run();
