# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore and build
dotnet restore
dotnet build

# Run the API (development)
dotnet run --project src/VoiceAgent.Api

# Run with Docker (API + Worker + PostgreSQL)
docker compose up --build

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> --project src/VoiceAgent.Infrastructure --startup-project src/VoiceAgent.Api

# Apply migrations manually (also runs automatically on startup)
dotnet ef database update --project src/VoiceAgent.Infrastructure --startup-project src/VoiceAgent.Api
```

**Health check:** `GET http://localhost:8080/api/health`  
**Swagger UI:** `http://localhost:8080/swagger`

There are no automated tests in this project yet.

## Architecture

Six-project Clean Architecture (.NET 8):

```
VoiceAgent.Common       → primitives (BaseEntity, ApiResponse<T>, Result<T>)
VoiceAgent.Domain       → entities, enums, value objects (no external dependencies)
VoiceAgent.Application  → service interfaces, DTOs, business orchestration
VoiceAgent.Infrastructure → EF Core/PostgreSQL, all external provider implementations
VoiceAgent.Api          → ASP.NET Core controllers, middleware, WebSocket endpoints
VoiceAgent.WorkerService → background jobs (outbound dialers, recording processing)
```

**Dependency rule:** `Api → Application → Domain ← Infrastructure`. Infrastructure implements Application interfaces; nothing references Infrastructure directly except the composition root in `Program.cs`.

**DI registration** is layered — each project exposes an extension method registered in `Program.cs`:
```csharp
builder.Services
    .AddCommon()
    .AddApplication()
    .AddInfrastructure(config)
    .AddApiPresentation();
```

## Key Subsystems

### Conversation Orchestration
`ConversationOrchestratorService` (~900 lines) is the core state machine. It handles 7 campaign types (`CampaignType` enum): `RestaurantOrder`, `CourierService`, `CabBooking`, `DoctorAppointment`, `MedicareSales`, `AcaSales`, `FeSales`. Each campaign progresses through `ConversationState`: `Greeting → CollectingSlots → SavingResult → Completed`. Collected data and final results are stored as JSONB (`CollectedSlotsJson`, `FinalResultJson`) on `CallSession`.

### Real-Time Voice (WebSocket)
Two WebSocket endpoints in `Program.cs` (not controllers) handle full-duplex audio streaming:
- `/api/voice/web-stream` — browser microphone
- `/api/voice/phone-stream` — telephony (Telnyx/FreeSwitch)

`VoiceStreamOrchestrator` drives: audio in → Deepgram STT → orchestrator → ElevenLabs TTS → audio out. Supports barge-in detection.

### Provider Abstraction
All external providers are behind interfaces in `VoiceAgent.Application/Providers/`:
- `ILlmProvider` → Gemini (`gemini-1.5-flash`)
- `ISpeechToTextProvider` → Deepgram
- `ITextToSpeechProvider` → ElevenLabs
- `IGeocodingProvider` → Nominatim
- `IRoutingProvider` → OSRM
- `IObjectStorageProvider` → Cloudflare R2
- `ITelephonyProvider` → Telnyx / FreeSwitch

Set `FeatureFlags:UseMockProviders: true` (default in `appsettings.Development.json`) to swap all providers for in-memory stubs — no real API keys needed for local dev.

### Multi-Tenancy
Every entity carries `TenantId`. The hierarchy is `Tenant → Client → Branch → Campaign → CallSession`. Queries must always filter by `TenantId`.

### RAG
`KnowledgeBase → KnowledgeDocument → KnowledgeChunk` stores embedded documents. `IRagRetrievalService` performs vector similarity search. Controlled by `FeatureFlags:EnableRag`.

## Database

- PostgreSQL 16 via Npgsql / EF Core 8
- `AppDbContext` in `src/VoiceAgent.Infrastructure/Persistence/AppDbContext.cs` (48 DbSets)
- Migrations auto-apply on startup (`db.Database.Migrate()` in `Program.cs`)
- JSONB columns used for: `CollectedSlotsJson`, `FinalResultJson`, `SummaryJson`, `RequiredSlotsJson`, `AllowedToolsJson`, `EmbeddingJson`
- Seeder (`DatabaseSeeder.SeedAsync`) runs only in Development

**Local connection string** (appsettings.json):
```
Host=localhost;Port=5432;Database=voice_agent;Username=postgres;Password=...
```

## API Conventions

- All responses use `ApiResponse<T>` envelope: `{ success, data, message }`
- Controllers are thin — delegate immediately to an Application service
- Routes follow `api/<resource>` pattern
- Demo flow (no auth required): `POST /api/demo/start` → `POST /api/demo/message` → `POST /api/demo/end`

## Feature Flags (appsettings.json)

| Flag | Default (Dev) | Effect |
|---|---|---|
| `UseMockProviders` | `true` | All external providers use stubs |
| `EnableRag` | `true` | Knowledge base retrieval active |
| `EnableCallRecording` | `false` | Audio storage off |

## Important Files

| Path | Purpose |
|---|---|
| `src/VoiceAgent.Api/Program.cs` | Bootstrap, DI wiring, WebSocket endpoints |
| `src/VoiceAgent.Application/Services/ConversationOrchestratorService.cs` | Core conversation state machine |
| `src/VoiceAgent.Application/Services/VoiceStreamOrchestrator.cs` | Real-time audio pipeline |
| `src/VoiceAgent.Infrastructure/Persistence/AppDbContext.cs` | EF Core context (48 DbSets) |
| `src/VoiceAgent.Infrastructure/Persistence/Migrations/` | EF migrations |
| `docs/VOICE_MODE_FLOW.md` | Frontend WebSocket integration spec |
| `docs_solution_structure.md` | Architecture decisions and bootstrap checklist |
| `docker-compose.yml` | Local dev stack (API + Worker + PostgreSQL) |
