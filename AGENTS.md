# AGENTS.md

## Scope
These instructions apply to the entire repository.

## Project status
- This repository is currently in **planning/bootstrap mode**.
- Do **not** start application code implementation unless explicitly requested.
- Prioritize creating/maintaining Codex-readable process assets (skills, planning docs, templates).

## How the agent should work here
1. Read relevant `SKILL.md` files in `/skills/**` before making substantial changes.
2. Keep changes small, reviewable, and aligned to Clean Architecture boundaries.
3. For implementation requests, prefer generating compile-ready skeletons before feature depth.
4. Preserve tenant/client/campaign isolation assumptions in all planning artifacts.

## Allowed work right now
- Create/update Codex skill folders and `SKILL.md` files.
- Create/update agent instruction files and planning templates.

## Not allowed right now (unless user asks explicitly)
- Building production feature code.
- Introducing external provider credentials/secrets.
- Large refactors to unrelated files.


## Local skills available in this repo
- `skills/backend-bootstrap/SKILL.md`
- `skills/backend-feature-implementation/SKILL.md`
- `skills/call-flow-pipeline-optimization/SKILL.md`
