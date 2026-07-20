---
name: tunit-test-runner
description: >-
  Run, filter, and select TUnit tests through `dotnet test`. Use this whenever
  executing .NET tests in a project that depends on TUnit (built on
  Microsoft.Testing.Platform / MTP, not VSTest), when `dotnet test --filter`
  reports "Zero tests ran", or when tests must be narrowed by assembly,
  namespace, class, test name, [Category], or other custom properties. Covers
  the `--treenode-filter` path-based query syntax, its operators, the `--`
  separator rules across SDK versions, and common 0-tests troubleshooting.
license: MIT
---

# Running TUnit tests with `dotnet test`

## The one rule that matters most

TUnit runs on **Microsoft.Testing.Platform (MTP)**, not VSTest. The reflexive
`dotnet test --filter "Category=X"` **does not work**: MTP silently rejects the
`--filter` flag, prints its own help text, and exits with `Zero tests ran`. That
looks like a passing-but-empty run or a config failure, but it is just an
unrecognised flag. **Never reach for `--filter` on a TUnit project.** Use
`--treenode-filter` instead.

| Other frameworks (VSTest)                  | TUnit (MTP)                                          |
| ------------------------------------------ | ---------------------------------------------------- |
| `--filter "Category=Integration"`          | `--treenode-filter "/*/*/*/*[Category=Integration]"` |
| `--filter "FullyQualifiedName~LoginTests"` | `--treenode-filter "/*/*/LoginTests/*"`              |
| `--filter "Name=AcceptCookiesTest"`        | `--treenode-filter "/*/*/*/AcceptCookiesTest"`       |

## How to invoke it

Prefer `dotnet test` over `dotnet run`: `dotnet test` builds and runs every
targeted TFM automatically and works against a `.csproj`, `.sln`, or `.slnx`,
whereas `dotnet run` only runs a single TFM.

The catch is the `--` separator, which depends on the SDK:

```bash
# Universal form — works on every SDK. Use this by default.
dotnet test -- --treenode-filter "/*/*/LoginTests/*"

# .NET 10+ SDK only — the platform flag can be passed directly.
dotnet test --treenode-filter "/*/*/LoginTests/*"
```

Anything after `--` is passed through to the TUnit test runner rather than to
the `dotnet test` command itself. Flags from extension packages
(`--coverage`, `--report-trx`, `--results-directory`, etc.) **must** also sit
after the `--`:

```bash
dotnet test --configuration Release --no-build \
  -- --treenode-filter "/*/*/*/*[Category=Unit]" --coverage --report-trx
```

> Run with no filter to execute everything: `dotnet test`.

## The `--treenode-filter` syntax

A filter is a path with four segments, optionally annotated with a property
group on any segment:

```
/<Assembly>/<Namespace>/<Class>/<TestName>[Property=Value]
```

Use `*` as a wildcard in any segment. The classic "run all tests" filter is
`/*/*/*/*` — four wildcards, one per level.

### Operators

| Operator | Meaning                                       | Example                                |
| -------- | --------------------------------------------- | -------------------------------------- |
| `*`      | Wildcard within a segment                     | `/*/*/LoginTests*/*`                   |
| `=`      | Property equals (exact)                       | `/*/*/*/*[Category=Unit]`              |
| `!=`     | Property not equal (exclude)                  | `/*/*/*/*[Category!=Slow]`             |
| `&`      | AND — within one segment / property group     | `/**[(Category=Unit)&(Priority=High)]` |
| `\|`     | OR — within one segment / property group      | `/*/*/(LoginTests)\|(SignupTests)/*`   |
| `**`     | Match any path depth (must be at the **end**) | `/MyAssembly/**`                       |

### Two grammar rules that are easy to get wrong

1. **`&` and `|` operate _inside a single segment or property group_, and each
   side must be wrapped in parentheses.** They do not join two complete paths.
2. **Only one property group `[...]` is allowed per path segment.** Combine
   conditions _inside_ the single bracket — do not chain brackets.
3. **`**` must terminate the path.** `/MyAssembly/**` is valid; `/**/Class/*`
   is not.

## Troubleshooting: "Zero tests ran" / 0 tests discovered

Check, in order:

1. **`--filter` was used instead of `--treenode-filter`.** This is the most common cause.
2. **`Microsoft.NET.Test.Sdk` is still referenced.** It conflicts with the TUnit MTP platform.
3. **TUnit package missing.** Ensure `<PackageReference Include="TUnit" Version="*" />`.
4. **Missing `[Test]` attribute** or unsupported method shape.
5. **Wrong `OutputType`.** A `hostfxr.dll could not be found` error means the project needs `<OutputType>Exe</OutputType>`.
6. **Bad filter shape.** Remember the path has exactly four segments.

## References

- TUnit — Test Filters: https://tunit.dev/docs/execution/test-filters/
- TUnit — Troubleshooting & FAQ: https://tunit.dev/docs/troubleshooting/
- TUnit — CI/CD pipelines: https://tunit.dev/docs/examples/tunit-ci-pipeline/
- TUnit — Explicit tests: https://tunit.dev/docs/writing-tests/explicit/
- MTP graph-query filtering spec: https://github.com/microsoft/testfx/blob/main/docs/mstest-runner-graphqueryfiltering/graph-query-filtering.md
