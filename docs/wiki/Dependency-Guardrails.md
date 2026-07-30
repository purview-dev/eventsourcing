# Dependency Guardrails

This page documents repository guardrails that prevent known dependency/runtime pitfalls.

## ZodSharp direct-reference guardrail

### Problem

When a consumer project directly references the `ZodSharpImpl` project and uses `ZodSharp` types, relying on transitive package flow can lead to runtime assembly load failures (for example, `FileNotFoundException` for `ZodSharp`).

### Required fix in consuming project

Add a direct package reference:

```xml
<PackageReference Include="ZodSharp" />
```

### Automated enforcement

`Purview.EventSourcing.ZodSharp` includes a build target in package `buildTransitive` assets (`buildTransitive/Purview.EventSourcing.ZodSharp.targets`):

- Target name: `ValidateZodSharpDirectReference`
- Runs: `BeforeTargets="ResolveReferences"`
- Behavior:
  - Detects projects that reference `ZodSharpImpl` via `ProjectReference`
  - Fails the build if `PackageReference Include="ZodSharp"` is missing
  - Emits a remediation message with the exact package reference to add

This shifts failure left from runtime to build-time.

### CI verification

The reusable pack workflow also validates the generated `.nupkg` and fails if `buildTransitive/Purview.EventSourcing.ZodSharp.targets` is missing from the package contents.

## Validation adapters overview

- `Purview.EventSourcing.FluentValidation`: adapter for `FluentValidation.IValidator<T>` to `IAggregateValidator<T>`.
- `Purview.EventSourcing.ZodSharp`: adapter for `ZodSharp` schema validation to `IAggregateValidator<T>`.

When using either adapter package directly from source projects, keep direct package references explicit for external runtime dependencies used by the adapter.
