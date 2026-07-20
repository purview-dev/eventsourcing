# Agent Instructions

## Purpose

This repository contains the `Purview.EventSourcing` framework, storage providers, source generators, sample applications, and TUnit-based test suites.

Use these instructions for any work in this solution.

## Source of truth and layout

- Solution file: `src/Purview.EventSourcing.slnx`
- Packable/source projects: `src/src/**`
- Tests: `src/tests/**`
- Wiki/docs: `docs/wiki/**`
- Repo-local skills: `.agents/skills/**`
- Repo-local Copilot bootstrap instructions: `.github/copilot-instructions.md`

## Core behavior

- Keep changes focused, reviewable, and consistent with existing patterns.
- Follow repository conventions before introducing new abstractions.
- Update tests and docs whenever behavior, provider support, or query semantics change.
- Never read, copy, or store secrets from excluded files.
- Treat event streams as the source of truth; snapshots are optimization/read models only.

## Coding conventions

- Prefer the repository's existing event-sourcing/source-generator patterns.
- Aggregates should remain `partial` and use `[GenerateAggregate]` plus generated event methods where applicable.
- Prefer `EventStoreList<T>` / `EventStoreSet<T>` for aggregate collection state that participates in generated events.
- Use `[Computed]` parameters for derived event values that callers must not set directly.
- Keep invariants in aggregate hooks/value objects rather than scattered in services.
- Avoid unrelated refactors while fixing a targeted issue.

## Snapshot/query guidance

- SQL snapshot queries can translate deep predicates over directly mapped complex JSON graphs.
- Provider-converted scalar value objects may not support deep member translation through `.Value`.
- If deep SQL predicates are required for a complex scalar concept, prefer a separately mapped complex mirror property derived from canonical state and covered by integration tests.
- Document any provider-specific translation limitation or newly supported shape in `docs/wiki/Provider-Feature-Matrix.md` and provider docs.

## Testing conventions

- This repo uses **TUnit** on Microsoft.Testing.Platform.
- Do **not** use `dotnet test --filter` for TUnit projects; use `--treenode-filter` when narrowing scope.
- After code changes, run the most relevant `dotnet test` command before finishing.
- For SQL/Mongo/Azure/Cosmos integration tests, assume container or provider infrastructure may be required.

## Docs and instruction maintenance

- Keep `docs/wiki/Home.md`, `docs/wiki/Provider-Feature-Matrix.md`, and provider-specific guides aligned when capabilities change.
- Keep `.agents/skills/event-sourcing-*` guidance aligned with actual framework behavior.
- If repo-local instruction/skill locations change, update both this file and `.github/copilot-instructions.md`.

## Useful local workflow facts

- Restore/build/test commonly target `src/Purview.EventSourcing.slnx`.
- `Justfile` contains wrapper recipes for restore, build, test, pack, and lint.
- `package.json` version is the release/package version source of truth.
