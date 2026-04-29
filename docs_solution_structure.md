# VoiceAgent Backend Solution Structure

## Proposed Solution Layout

```text
VoiceAgent.sln
src/
  VoiceAgent.Domain/
  VoiceAgent.Application/
  VoiceAgent.Infrastructure/
  VoiceAgent.Common/
  VoiceAgent.WebAPI/
  VoiceAgent.WorkerService/
```

## Project Responsibilities

### `VoiceAgent.Domain`
Pure business models and rules only:
- Entities
- Enums
- Value objects
- Domain events
- Domain rules

**Must not include:** EF Core, HTTP concerns, provider SDK types, logging implementations.

### `VoiceAgent.Application`
Use cases and orchestration layer:
- DTOs
- Service interfaces
- Business services
- Conversation orchestrator
- Campaign engine
- Tool contracts
- Pricing services
- Validation services
- RAG service interfaces

**Must not include:** database-specific code, provider-specific models.

### `VoiceAgent.Infrastructure`
External integrations and technical implementations:
- EF Core `DbContext`
- PostgreSQL repositories
- Deepgram client
- ElevenLabs client
- Gemini client
- FreeSWITCH/Telnyx clients
- Nominatim/OSRM clients
- Cloudflare R2 client
- Serilog setup
- In-memory conversation state store

### `VoiceAgent.Common`
Reusable low-level primitives:
- `BaseEntity`
- `AuditableEntity`
- `Result<T>`
- `ApiResponse<T>`
- `PagedResult<T>`
- `PaginationRequest`
- Correlation ID helpers
- DateTime provider
- Guard helpers

**Rule:** Do not place business logic in this project.

### `VoiceAgent.WebAPI`
Thin API entry point:
- Controllers/minimal endpoints
- Middleware
- Auth (later)
- Swagger
- WebSocket endpoints
- Request validation

**Rule:** business logic must execute through `Application` services.

### `VoiceAgent.WorkerService`
Background processing:
- Outbound dialer
- Failed external API retry
- Call summary generation
- Recording processing
- RAG indexing
- Billing aggregation
- Cleanup jobs

## Dependency Direction

```text
WebAPI -> Application -> Domain
Infrastructure -> Application + Domain + Common
WorkerService -> Application
```

## Bootstrap Conventions Checklist

- Keep controllers thin; map requests to application commands/services.
- Keep one DTO per API contract when practical for clarity.
- Use `ApiResponse<T>` envelope at boundaries where standardized API responses are needed.
- Keep provider and database details isolated to Infrastructure.
- Register Infrastructure implementations via DI; Application depends on abstractions only.
- Preserve tenant/client/campaign isolation assumptions in all future design artifacts.

## Suggested Next Execution Sequence

1. Create solution and `src/` folder structure.
2. Add empty projects with references aligned to dependency direction.
3. Add base primitives in `VoiceAgent.Common`.
4. Add core entities/value objects/events in `VoiceAgent.Domain`.
5. Add use-case interfaces/DTO contracts in `VoiceAgent.Application`.
6. Add Infrastructure adapters and DI registration stubs.
7. Add thin WebAPI and WorkerService entry points wired to Application.
