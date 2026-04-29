# Skill: backend-feature-implementation

## Purpose
Use this skill for incremental implementation of backend features in the VoiceAgent platform while maintaining Clean Architecture constraints.

## When to use
- User asks to implement a specific backend feature.
- User asks to add services/controllers/entities for a campaign flow.
- User asks to wire provider abstractions or infrastructure adapters.

## Guardrails
- Never place business logic inside controllers.
- Never leak provider SDK models into Application/Domain.
- Always enforce tenant/client/campaign scoping where relevant.
- Keep orchestration readable and state-driven.

## Implementation workflow
1. Define feature boundary and affected layers.
2. Add/update Application contracts (DTOs, interfaces, service methods).
3. Add/update Domain entities/value objects if required.
4. Implement Infrastructure repositories/providers.
5. Wire DI in each project's `DependencyInjection.cs`.
6. Add/update WebAPI endpoints as thin wrappers.
7. Add tests for happy path and isolation rules.

## Done criteria
- Build passes.
- Feature paths covered by tests.
- API returns `ApiResponse<T>`.
- No cross-tenant data leakage risk in queries.
