# Skill: backend-bootstrap

## Purpose
Use this skill when the user asks to initialize or scaffold the VoiceAgent backend structure without deep feature implementation.

## When to use
- User asks for initial setup/skeleton.
- User asks for project structure aligned with Clean Architecture.
- User asks for dependency injection boundaries and DTO conventions.

## Inputs expected
- Target .NET version (default .NET 8)
- Required projects/modules
- Any fixed architecture constraints

## Workflow
1. Confirm repository phase (planning vs implementation).
2. Generate/update structure docs or scaffold plan first.
3. Ensure conventions are captured:
   - one DTO file per API
   - thin controllers
   - infrastructure-only external/db calls
   - `ApiResponse<T>` response envelope
4. Create only requested artifacts; avoid feature overreach.
5. Summarize what is prepared for the next coding phase.

## Output checklist
- Clear folder/project map
- Convention checklist
- Next-step execution sequence
