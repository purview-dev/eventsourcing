# Break FluentValidation Dependency — Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Remove the hard dependency on FluentValidation from the core `Purview.EventSourcing` package, replacing it with framework-agnostic validation abstractions that default to DataAnnotations, and move FluentValidation integration into a separate `Purview.EventSourcing.FluentValidation` package.

**Architecture:** The core package defines its own `ValidationResult`, `ValidationFailure`, and `ValidationException` types in a `Purview.EventSourcing.Validation` namespace. `IAggregateValidator<TAggregate>` returns the core `ValidationResult`. `DefaultAggregateValidator<TAggregate>` uses `System.ComponentModel.DataAnnotations.Validator` directly (no FluentValidation inheritance). Store constructors accept `IAggregateValidator<TAggregate>?` instead of `FluentValidation.IValidator<T>?`. A new `Purview.EventSourcing.FluentValidation` package provides an adapter that wraps `FluentValidation.IValidator<T>` and maps its results to the core types, plus DI extensions for registration.

**Tech Stack:** .NET 10, C# 13, System.ComponentModel.DataAnnotations (framework), FluentValidation 12.x (optional, in separate package), TUnit for tests, TUnit.Mocks for mocking.

---

## Current State Summary

FluentValidation is referenced directly by `src/src/EventSourcing/EventSourcing.csproj` and used in:

1. **`IAggregateValidator<TAggregate>`** (`src/src/EventSourcing/Services/IAggregateValidator.cs`) — returns `FluentValidation.Results.ValidationResult`
2. **`DefaultAggregateValidator<TAggregate>`** (`src/src/EventSourcing/Services/DefaultAggregateValidator.cs`) — extends `FluentValidation.AbstractValidator<TAggregate>`, uses `RuleFor`/`Custom`
3. **`FluentValidationAggregateValidator<TAggregate>`** (`src/src/EventSourcing/Services/FluentValidationAggregateValidator.cs`) — wraps `FluentValidation.IValidator<T>`
4. **`AggregateValidatorAdapter`** (`src/src/EventSourcing/Services/AggregateValidatorAdapter.cs`) — adapts `FluentValidation.IValidator<T>` → `IAggregateValidator<T>`
5. **`SaveResult<TAggregate>`** (`src/src/EventSourcing/Aggregates/SaveResult.cs`) — constructor takes `FluentValidation.Results.ValidationResult`, `EnsureValid()` throws `FluentValidation.ValidationException`
6. **All 4 store constructors** (InMemory, SqlServer, MongoDB, AzureStorage) — accept `FluentValidation.IValidator<T>? validator = null`
7. **All 4 store SaveAsync methods** — use `FluentValidation.Results.ValidationResult` for `ReturnSaveResult`
8. **Tests** — construct `SaveResult<T>` with `FluentValidation.Results.ValidationResult()` and `FluentValidation.Results.ValidationFailure`
9. **Samples.QuickStart** — uses `FluentValidation.Results.ValidationResult`

---

## Task List

### Task 1: Create core validation abstractions

**Objective:** Create framework-agnostic `ValidationResult`, `ValidationFailure`, and `ValidationException` types in the core package.

**Files:**
- Create: `src/src/EventSourcing/Validation/ValidationFailure.cs`
- Create: `src/src/EventSourcing/Validation/ValidationResult.cs`
- Create: `src/src/EventSourcing/Validation/ValidationException.cs`

**Step 1: Create `ValidationFailure.cs`**

```csharp
namespace Purview.EventSourcing.Validation;

/// <summary>
/// Represents a single validation failure.
/// </summary>
public sealed record ValidationFailure
{
	/// <summary>
	/// The name of the property that failed validation, or <see langword="null"/> if the failure is aggregate-wide.
	/// </summary>
	public string? PropertyName { get; init; }

	/// <summary>
	/// The error message describing the failure.
	/// </summary>
	public string? ErrorMessage { get; init; }

	/// <summary>
	/// Constructs a new <see cref="ValidationFailure"/>.
	/// </summary>
	public ValidationFailure(string? propertyName, string? errorMessage)
	{
		PropertyName = propertyName;
		ErrorMessage = errorMessage;
	}
}
```

**Step 2: Create `ValidationResult.cs`**

```csharp
namespace Purview.EventSourcing.Validation;

/// <summary>
/// Represents the result of validating an aggregate.
/// </summary>
public sealed class ValidationResult
{
	/// <summary>
	/// A static instance representing a successful validation with no errors.
	/// </summary>
	public static ValidationResult Success { get; } = new([]);

	readonly IReadOnlyList<ValidationFailure> _errors;

	/// <summary>
	/// Constructs a new <see cref="ValidationResult"/> with the given failures.
	/// </summary>
	/// <param name="failures">The validation failures. Pass an empty collection for a successful result.</param>
	public ValidationResult(IEnumerable<ValidationFailure> failures)
	{
		_errors = [.. failures];
	}

	/// <summary>
	/// Constructs a new successful <see cref="ValidationResult"/> with no errors.
	/// </summary>
	public ValidationResult() : this([]) { }

	/// <summary>
	/// <see langword="true"/> when there are no <see cref="Errors"/>; otherwise <see langword="false"/>.
	/// </summary>
	public bool IsValid => _errors.Count == 0;

	/// <summary>
	/// The collection of <see cref="ValidationFailure"/>s produced during validation.
	/// </summary>
	public IReadOnlyList<ValidationFailure> Errors => _errors;
}
```

**Step 3: Create `ValidationException.cs`**

```csharp
namespace Purview.EventSourcing.Validation;

/// <summary>
/// Thrown when aggregate validation fails (e.g. via <see cref="Aggregates.SaveResult{TAggregate}.EnsureValid"/>).
/// </summary>
public sealed class ValidationException : Exception
{
	/// <summary>
	/// The validation failures that caused this exception.
	/// </summary>
	public IReadOnlyList<ValidationFailure> Errors { get; }

	/// <summary>
	/// Constructs a new <see cref="ValidationException"/> with the given failures.
	/// </summary>
	public ValidationException(IEnumerable<ValidationFailure> errors)
		: base("Validation failed: " + string.Join("; ", errors.Select(e => e.ErrorMessage)))
	{
		Errors = [.. errors];
	}

	/// <summary>
	/// Constructs a new <see cref="ValidationException"/> with the given message and failures.
	/// </summary>
	public ValidationException(string message, IEnumerable<ValidationFailure> errors)
		: base(message)
	{
		Errors = [.. errors];
	}
}
```

**Step 4: Build to verify compilation**

Run: `dotnet build src/src/EventSourcing/EventSourcing.csproj`
Expected: PASS (new files compile, no conflicts)

**Step 5: Commit**

```bash
git add src/src/EventSourcing/Validation/
git commit -m "feat: add framework-agnostic validation abstractions"
```

---

### Task 2: Update `IAggregateValidator<TAggregate>` to use core types

**Objective:** Change the interface to return `Purview.EventSourcing.Validation.ValidationResult` instead of `FluentValidation.Results.ValidationResult`.

**Files:**
- Modify: `src/src/EventSourcing/Services/IAggregateValidator.cs`

**Step 1: Update the interface**

Replace the entire file contents:

```csharp
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.Services;

public interface IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	ValidationResult Validate(TAggregate aggregate);

	Task<ValidationResult> ValidateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
```

**Step 2: Build — expect failures in `DefaultAggregateValidator`, `FluentValidationAggregateValidator`, `AggregateValidatorAdapter`, and `SaveResult`**

Run: `dotnet build src/src/EventSourcing/EventSourcing.csproj`
Expected: FAIL — compilation errors in the files that will be updated in subsequent tasks

**Step 3: Commit (with broken build — will be fixed in following tasks)**

```bash
git add src/src/EventSourcing/Services/IAggregateValidator.cs
git commit -m "refactor: IAggregateValidator returns core ValidationResult"
```

---

### Task 3: Rewrite `DefaultAggregateValidator<TAggregate>` without FluentValidation

**Objective:** Remove the `AbstractValidator<TAggregate>` base class dependency. Use `System.ComponentModel.DataAnnotations.Validator.TryValidateObject` directly and map to the core `ValidationResult`.

**Files:**
- Modify: `src/src/EventSourcing/Services/DefaultAggregateValidator.cs`

**Step 1: Rewrite the file**

```csharp
using System.ComponentModel.DataAnnotations;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.Services;

/// <summary>
/// A default validator for <see cref="IAggregate"/>'s based on
/// standard data annotations.
/// </summary>
public sealed class DefaultAggregateValidator<TAggregate>
	: IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	/// <summary>
	/// A statically cached instance based on the use of standard data annotations.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
	public static IAggregateValidator<TAggregate> Instance { get; } = new DefaultAggregateValidator<TAggregate>();

	public ValidationResult Validate(TAggregate aggregate)
	{
		ArgumentNullException.ThrowIfNull(aggregate);

		var failures = ValidateWithAnnotations(aggregate);
		return new ValidationResult(failures);
	}

	public Task<ValidationResult> ValidateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(aggregate);

		var failures = ValidateWithAnnotations(aggregate);
		return Task.FromResult(new ValidationResult(failures));
	}

	static IEnumerable<ValidationFailure> ValidateWithAnnotations(TAggregate aggregate)
	{
		ValidationContext daContext = new(aggregate);
		List<System.ComponentModel.DataAnnotations.ValidationResult> failures = [];

		if (
			!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
				aggregate,
				daContext,
				failures,
				true
			)
		)
		{
			foreach (var failure in failures)
				yield return new ValidationFailure(
					failure.MemberNames.FirstOrDefault(),
					failure.ErrorMessage
				);
		}
	}
}
```

**Step 2: Commit**

```bash
git add src/src/EventSourcing/Services/DefaultAggregateValidator.cs
git commit -m "refactor: DefaultAggregateValidator uses DataAnnotations directly"
```

---

### Task 4: Update `SaveResult<TAggregate>` to use core validation types

**Objective:** Replace `FluentValidation.Results.ValidationResult` and `FluentValidation.ValidationException` with the core abstractions.

**Files:**
- Modify: `src/src/EventSourcing/Aggregates/SaveResult.cs`

**Step 1: Update imports and types**

Replace lines 1-3 (the using block) and line 44 (constructor parameter type), line 61 (property type), line 114 (EnsureValid throw):

Replace:
```csharp
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.Results;
```

With:
```csharp
using System.Diagnostics.CodeAnalysis;
using Purview.EventSourcing.Validation;
```

Replace the constructor parameter (line 44):
```csharp
	public SaveResult(TAggregate aggregate, ValidationResult validationResult, bool saved, bool skipped)
```
(stays the same — but `ValidationResult` now resolves to `Purview.EventSourcing.Validation.ValidationResult`)

Replace the property (line 61):
```csharp
	public ValidationResult ValidationResult { get; }
```
(stays the same — now resolves to core type)

Replace `EnsureValid()` (line 111-115):
```csharp
	public void EnsureValid()
	{
		if (!IsValid)
			throw new ValidationException(ValidationResult.Errors);
	}
```
(stays the same — `ValidationException` now resolves to `Purview.EventSourcing.Validation.ValidationException`)

**Step 2: Build to verify the core EventSourcing project compiles**

Run: `dotnet build src/src/EventSourcing/EventSourcing.csproj`
Expected: FAIL — `FluentValidationAggregateValidator.cs` and `AggregateValidatorAdapter.cs` still reference FluentValidation types and the old `IAggregateValidator` signature. These are fixed in the next task.

**Step 3: Commit**

```bash
git add src/src/EventSourcing/Aggregates/SaveResult.cs
git commit -m "refactor: SaveResult uses core Validation types"
```

---

### Task 5: Remove `FluentValidationAggregateValidator` and `AggregateValidatorAdapter` from core

**Objective:** These two files are FluentValidation-specific and belong in the new separate package. Remove them from the core project for now (they will be recreated in the new package in Task 10).

**Files:**
- Delete: `src/src/EventSourcing/Services/FluentValidationAggregateValidator.cs`
- Delete: `src/src/EventSourcing/Services/AggregateValidatorAdapter.cs`

**Step 1: Delete the files**

Use terminal or file operations to remove:
- `src/src/EventSourcing/Services/FluentValidationAggregateValidator.cs`
- `src/src/EventSourcing/Services/AggregateValidatorAdapter.cs`

**Step 2: Build the core project**

Run: `dotnet build src/src/EventSourcing/EventSourcing.csproj`
Expected: FAIL — the 4 store projects (InMemory, SqlServer, MongoDB, AzureStorage) reference `AggregateValidatorAdapter.Adapt()` and `FluentValidation.IValidator<T>` in their constructors. These are fixed in Tasks 6-9.

**Step 3: Commit**

```bash
git add -A src/src/EventSourcing/Services/
git commit -m "refactor: remove FluentValidation-specific classes from core"
```

---

### Task 6: Update InMemoryEventStore constructor and save logic

**Objective:** Change the store constructor to accept `IAggregateValidator<T>?` instead of `FluentValidation.IValidator<T>?`, and remove the `AggregateValidatorAdapter.Adapt()` call.

**Files:**
- Modify: `src/src/InMemory/Events/InMemoryEventStore.cs` (lines 3, 13, 21)
- Modify: `src/src/InMemory/Snapshots/InMemorySnapshotStore.cs` (line 12, 18)

**Step 1: Update `InMemoryEventStore.cs`**

Replace line 3:
```csharp
using FluentValidation.Results;
```
With:
```csharp
using Purview.EventSourcing.Validation;
```

Replace lines 10-14 (constructor signature):
```csharp
public partial class InMemoryEventStore<T>(
	ChangeFeed.IAggregateChangeFeedNotifier<T> aggregateChangeNotifier,
	IAggregateRequirementsManager aggregateRequirementsManager,
	FluentValidation.IValidator<T>? validator = null,
	IAggregateIdFactory? aggregateIdFactory = null
) : IInMemoryEventStore<T>, IDisposable
```
With:
```csharp
public partial class InMemoryEventStore<T>(
	ChangeFeed.IAggregateChangeFeedNotifier<T> aggregateChangeNotifier,
	IAggregateRequirementsManager aggregateRequirementsManager,
	IAggregateValidator<T>? validator = null,
	IAggregateIdFactory? aggregateIdFactory = null
) : IInMemoryEventStore<T>, IDisposable
```

Replace line 21:
```csharp
	readonly IAggregateValidator<T>? _validator = AggregateValidatorAdapter.Adapt(validator);
```
With:
```csharp
	readonly IAggregateValidator<T>? _validator = validator;
```

Update `ReturnSaveResult` local function (around line 304-305) — change `ValidationResult?` to use core type:
```csharp
		static SaveResult<T> ReturnSaveResult(
			T a,
			bool success,
			bool skipped,
			ValidationResult? validationResult = null
		) => new(a, validationResult ?? new ValidationResult(), success, skipped);
```
(stays the same — `ValidationResult` now resolves to core type via the new `using`)

Update `GuardAsync` (line 400-407) — no signature change needed, already returns `ValidationResult` which now resolves to core type:
```csharp
	async Task<ValidationResult> GuardAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(aggregate, nameof(aggregate));

		return _validator == null
			? await DefaultAggregateValidator<T>.Instance.ValidateAsync(aggregate, cancellationToken)
			: await _validator.ValidateAsync(aggregate, cancellationToken);
	}
```
(stays the same)

**Step 2: Update `InMemorySnapshotStore.cs`**

Replace line 12:
```csharp
	FluentValidation.IValidator<T>? validator = null,
```
With:
```csharp
	IAggregateValidator<T>? validator = null,
```

The base class call on lines 16-19 passes `validator` directly — no change needed beyond the type.

**Step 3: Build InMemory project**

Run: `dotnet build src/src/InMemory/InMemory.csproj`
Expected: PASS (InMemory only depends on EventSourcing core)

**Step 4: Commit**

```bash
git add src/src/InMemory/Events/InMemoryEventStore.cs src/src/InMemory/Snapshots/InMemorySnapshotStore.cs
git commit -m "refactor: InMemory stores accept IAggregateValidator<T>"
```

---

### Task 7: Update SqlServerEventStore constructor and save logic

**Objective:** Same changes as Task 6 but for the SqlServer store.

**Files:**
- Modify: `src/src/SqlServer/Events/SqlServerEventStore.cs` (lines 45, 52)
- Modify: `src/src/SqlServer/Events/SqlServerEventStore.SaveAsync.cs` (line 6)

**Step 1: Update `SqlServerEventStore.cs`**

Replace line 45 (constructor parameter):
```csharp
		FluentValidation.IValidator<T>? validator = null,
```
With:
```csharp
		IAggregateValidator<T>? validator = null,
```

Replace line 52:
```csharp
		_validator = AggregateValidatorAdapter.Adapt(validator);
```
With:
```csharp
		_validator = validator;
```

**Step 2: Update `SqlServerEventStore.SaveAsync.cs`**

Replace line 6:
```csharp
using FluentValidation.Results;
```
With:
```csharp
using Purview.EventSourcing.Validation;
```

The `ReturnSaveResult` and `GuardAsync` methods already reference `ValidationResult` which will now resolve to the core type.

**Step 3: Build SqlServer project**

Run: `dotnet build src/src/SqlServer/SqlServer.csproj`
Expected: PASS

**Step 4: Commit**

```bash
git add src/src/SqlServer/Events/SqlServerEventStore.cs src/src/SqlServer/Events/SqlServerEventStore.SaveAsync.cs
git commit -m "refactor: SqlServer store accepts IAggregateValidator<T>"
```

---

### Task 8: Update MongoDBEventStore constructor and save logic

**Objective:** Same changes for the MongoDB store.

**Files:**
- Modify: `src/src/MongoDB/Events/MongoDBEventStore.cs` (lines 41, 47)
- Modify: `src/src/MongoDB/Events/MongoDBEventStore.SaveAsync.cs` (line 5)

**Step 1: Update `MongoDBEventStore.cs`**

Replace line 41 (constructor parameter):
```csharp
		FluentValidation.IValidator<T>? validator = null,
```
With:
```csharp
		IAggregateValidator<T>? validator = null,
```

Replace line 47:
```csharp
		_validator = AggregateValidatorAdapter.Adapt(validator);
```
With:
```csharp
		_validator = validator;
```

**Step 2: Update `MongoDBEventStore.SaveAsync.cs`**

Replace line 5:
```csharp
using FluentValidation.Results;
```
With:
```csharp
using Purview.EventSourcing.Validation;
```

**Step 3: Build MongoDB project**

Run: `dotnet build src/src/MongoDB/MongoDB.csproj`
Expected: PASS

**Step 4: Commit**

```bash
git add src/src/MongoDB/Events/MongoDBEventStore.cs src/src/MongoDB/Events/MongoDBEventStore.SaveAsync.cs
git commit -m "refactor: MongoDB store accepts IAggregateValidator<T>"
```

---

### Task 9: Update TableEventStore (AzureStorage) constructor and save logic

**Objective:** Same changes for the AzureStorage store.

**Files:**
- Modify: `src/src/AzureStorage/TableEventStore.cs` (lines 43, 52)
- Modify: `src/src/AzureStorage/TableEventStore.SaveAsync.cs` (line 7)

**Step 1: Update `TableEventStore.cs`**

Replace line 43 (constructor parameter):
```csharp
		FluentValidation.IValidator<T>? validator = null,
```
With:
```csharp
		IAggregateValidator<T>? validator = null,
```

Replace line 52:
```csharp
		_validator = AggregateValidatorAdapter.Adapt(validator);
```
With:
```csharp
		_validator = validator;
```

**Step 2: Update `TableEventStore.SaveAsync.cs`**

Replace line 7:
```csharp
using FluentValidation.Results;
```
With:
```csharp
using Purview.EventSourcing.Validation;
```

**Step 3: Build AzureStorage project**

Run: `dotnet build src/src/AzureStorage/AzureStorage.csproj`
Expected: PASS

**Step 4: Commit**

```bash
git add src/src/AzureStorage/TableEventStore.cs src/src/AzureStorage/TableEventStore.SaveAsync.cs
git commit -m "refactor: AzureStorage store accepts IAggregateValidator<T>"
```

---

### Task 10: Remove FluentValidation package reference from core EventSourcing.csproj

**Objective:** The core package no longer references FluentValidation.

**Files:**
- Modify: `src/src/EventSourcing/EventSourcing.csproj` (line 27)

**Step 1: Remove the PackageReference**

Delete line 27:
```xml
		<PackageReference Include="FluentValidation" />
```

**Step 2: Build the entire solution**

Run: `dotnet build src/Purview.EventSourcing.slnx`
Expected: FAIL — test projects and Samples.QuickStart still reference FluentValidation types. These are fixed in Tasks 11-13.

**Step 3: Commit**

```bash
git add src/src/EventSourcing/EventSourcing.csproj
git commit -m "refactor: remove FluentValidation dependency from core package"
```

---

### Task 11: Update unit tests to use core validation types

**Objective:** Replace all `FluentValidation.Results.ValidationResult` and `FluentValidation.Results.ValidationFailure` references in tests with the new core types.

**Files:**
- Modify: `src/tests/EventSourcing.UnitTests/EventStoreTransactionTests.cs` (many lines)
- Modify: `src/tests/EventSourcing.UnitTests/IEventStoreExtensionsEnlistTests.cs` (lines 30, 60, 90, 146, 239)
- Modify: `src/tests/EventSourcing.UnitTests/SqlServer/Snapshots/SqlServerSnapshotEventStoreTests.cs` (lines 188, 216)
- Delete: `src/tests/EventSourcing.UnitTests/Services/FluentValidationAggregateValidatorTests.cs` (moved to new test project in Task 14)

**Step 1: Update `EventStoreTransactionTests.cs`**

Add at the top (after existing usings):
```csharp
using Purview.EventSourcing.Validation;
```

Find-and-replace all occurrences of `FluentValidation.Results.ValidationResult` → `ValidationResult`
Find-and-replace all occurrences of `FluentValidation.Results.ValidationFailure` → `ValidationFailure`

The constructor `new ValidationResult(...)` works the same way (takes `IEnumerable<ValidationFailure>`).
The constructor `new ValidationFailure("Field", "Required")` works the same way.

Lines to update (non-exhaustive — use find-and-replace):
- Line 46: `new FluentValidation.Results.ValidationResult()` → `new ValidationResult()`
- Line 80: same
- Line 85: same
- Lines 121, 129, 202, 226, 354, 501, 506, 537, 579, 607, 806: same
- Lines 462-463:
  ```csharp
  var validationResult = new ValidationResult([
      new ValidationFailure("Field", "Required"),
  ]);
  ```

**Step 2: Update `IEventStoreExtensionsEnlistTests.cs`**

Add `using Purview.EventSourcing.Validation;` and replace all `FluentValidation.Results.ValidationResult` → `ValidationResult`.

Lines: 30, 60, 90, 146, 239.

**Step 3: Update `SqlServerSnapshotEventStoreTests.cs`**

Add `using Purview.EventSourcing.Validation;` and replace all `FluentValidation.Results.ValidationResult` → `ValidationResult`.

Lines: 188, 216.

**Step 4: Delete `FluentValidationAggregateValidatorTests.cs`**

This test will be recreated in the new FluentValidation test project (Task 14).

**Step 5: Build the test project**

Run: `dotnet build src/tests/EventSourcing.UnitTests/EventSourcing.UnitTests.csproj`
Expected: PASS (if EventSourcing.UnitTests.csproj has no direct FluentValidation PackageReference — it gets FluentValidation transitively through the EventSourcing project reference which no longer has it. Verify by checking the csproj.)

**Step 6: Run the tests**

Run: `dotnet test src/tests/EventSourcing.UnitTests/EventSourcing.UnitTests.csproj`
Expected: PASS — all tests pass with the core validation types

**Step 7: Commit**

```bash
git add src/tests/EventSourcing.UnitTests/
git commit -m "test: update unit tests to use core validation types"
```

---

### Task 12: Update Samples.QuickStart to use core validation types

**Objective:** Replace FluentValidation usage in the quickstart sample.

**Files:**
- Modify: `src/src/Samples.QuickStart/Infrastructure/InMemoryTransactionalEventStore.cs` (lines 9, 279, 282)

**Step 1: Update imports**

Replace line 9:
```csharp
using FluentValidation.Results;
```
With:
```csharp
using Purview.EventSourcing.Validation;
```

Lines 279 and 282 use `new ValidationResult()` which now resolves to the core type — no code change needed beyond the import.

**Step 2: Build the Samples.QuickStart project**

Run: `dotnet build src/src/Samples.QuickStart/Samples.QuickStart.csproj`
Expected: PASS

**Step 3: Commit**

```bash
git add src/src/Samples.QuickStart/Infrastructure/InMemoryTransactionalEventStore.cs
git commit -m "refactor: Samples.QuickStart uses core validation types"
```

---

### Task 13: Build the entire solution and verify no FluentValidation references remain in core

**Objective:** Confirm the core EventSourcing package and all storage providers compile without FluentValidation.

**Step 1: Build the solution (excluding test projects that may still need FluentValidation for the moved test)**

Run: `dotnet build src/Purview.EventSourcing.slnx`
Expected: PASS for all src projects. Test projects may fail if `FluentValidationAggregateValidatorTests.cs` was not deleted in Task 11. Verify it was deleted.

**Step 2: Verify no FluentValidation references in core**

Run:
```bash
grep -r "FluentValidation" src/src/EventSourcing/ --include="*.cs" --include="*.csproj"
```
Expected: No results

**Step 3: Commit if any remaining fixes needed**

```bash
git add -A
git commit -m "chore: verify no FluentValidation references in core"
```

---

### Task 14: Create the `Purview.EventSourcing.FluentValidation` package

**Objective:** Create a new NuGet package that provides FluentValidation integration with the event sourcing framework.

**Files:**
- Create: `src/src/FluentValidation/FluentValidation.csproj`
- Create: `src/src/FluentValidation/Services/FluentValidationAggregateValidator.cs`
- Create: `src/src/FluentValidation/Extensions/Microsoft/Extensions/DependencyInjection/ServiceCollectionExtensions.cs`

**Step 1: Create the project file**

`src/src/FluentValidation/FluentValidation.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup Label="NuGet Package Values">
		<IsPackable>true</IsPackable>
		<Title>Purview Event Sourcing FluentValidation Integration</Title>
		<Description>FluentValidation adapter for Purview EventSourcing — bridges FluentValidation IValidator&lt;T&gt; with IAggregateValidator&lt;T&gt;.</Description>
		<PackageId>Purview.EventSourcing.FluentValidation</PackageId>
		<PackageTags>$(PackageTags);purview;dotnet;event-sourcing;validation;fluentvalidation;</PackageTags>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="FluentValidation" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\EventSourcing\EventSourcing.csproj" />
	</ItemGroup>
</Project>
```

**Step 2: Create the adapter validator**

`src/src/FluentValidation/Services/FluentValidationAggregateValidator.cs`:
```csharp
using FluentValidation;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Services;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.FluentValidation;

/// <summary>
/// Adapts a <see cref="global::FluentValidation.IValidator{TAggregate}"/> to the
/// <see cref="IAggregateValidator{TAggregate}"/> interface, mapping FluentValidation
/// results to <see cref="ValidationResult"/>.
/// </summary>
public sealed class FluentValidationAggregateValidator<TAggregate>(IValidator<TAggregate> validator)
	: IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	readonly IValidator<TAggregate> _validator = validator;

	public ValidationResult Validate(TAggregate aggregate) =>
		Map(_validator.Validate(aggregate));

	public Task<ValidationResult> ValidateAsync(TAggregate aggregate, CancellationToken cancellationToken = default) =>
		_validator
			.ValidateAsync(aggregate, cancellationToken)
			.ContinueWith(t => Map(t.Result), cancellationToken);

	static ValidationResult Map(global::FluentValidation.Results.ValidationResult result)
	{
		if (result.IsValid)
			return ValidationResult.Success;

		var failures = result.Errors.Select(
			e => new ValidationFailure(e.PropertyName, e.ErrorMessage)
		);
		return new ValidationResult(failures);
	}
}
```

**Step 3: Create DI extensions**

`src/src/FluentValidation/Extensions/Microsoft/Extensions/DependencyInjection/ServiceCollectionExtensions.cs`:
```csharp
using System.ComponentModel;
using FluentValidation;
using Purview.EventSourcing.FluentValidation;
using Purview.EventSourcing.Services;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers a <see cref="FluentValidationAggregateValidator{TAggregate}"/> adapter
	/// for the specified aggregate type, wrapping the registered
	/// <see cref="IValidator{TAggregate}"/>.
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <typeparam name="TValidator">The FluentValidation validator implementation.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddFluentValidationAdapter<TAggregate, TValidator>(this IServiceCollection services)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
		where TValidator : class, IValidator<TAggregate>
	{
		services.AddSingleton<IValidator<TAggregate>, TValidator>();
		services.AddSingleton<IAggregateValidator<TAggregate>, FluentValidationAggregateValidator<TAggregate>>();
		return services;
	}

	/// <summary>
	/// Registers a <see cref="FluentValidationAggregateValidator{TAggregate}"/> adapter
	/// using a factory that resolves <see cref="IValidator{TAggregate}"/> from the container.
	/// Use this when validators are already registered (e.g. via <c>AddValidatorsFromAssembly</c>).
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddFluentValidationAdapter<TAggregate>(this IServiceCollection services)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
	{
		services.AddSingleton<IAggregateValidator<TAggregate>>(sp =>
		{
			var validator = sp.GetService<IValidator<TAggregate>>();
			return validator is null
				? null!
				: new FluentValidationAggregateValidator<TAggregate>(validator);
		});
		return services;
	}
}
```

**Step 4: Add the project to the solution**

Run:
```bash
cd src && dotnet sln Purview.EventSourcing.slnx add src/FluentValidation/FluentValidation.csproj
```

**Step 5: Build the new package**

Run: `dotnet build src/src/FluentValidation/FluentValidation.csproj`
Expected: PASS

**Step 6: Commit**

```bash
git add src/src/FluentValidation/ src/Purview.EventSourcing.slnx
git commit -m "feat: add Purview.EventSourcing.FluentValidation package"
```

---

### Task 15: Create FluentValidation unit test project

**Objective:** Create a test project for the FluentValidation adapter package and port the moved test.

**Files:**
- Create: `src/tests/FluentValidation.UnitTests/FluentValidation.UnitTests.csproj`
- Create: `src/tests/FluentValidation.UnitTests/Services/FluentValidationAggregateValidatorTests.cs`

**Step 1: Create the test project file**

`src/tests/FluentValidation.UnitTests/FluentValidation.UnitTests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<IsPackable>false</IsPackable>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="FluentValidation" />
		<PackageReference Include="TUnit" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\..\src\FluentValidation\FluentValidation.csproj" />
		<ProjectReference Include="..\..\src\EventSourcing\EventSourcing.csproj" />
	</ItemGroup>
</Project>
```

**Step 2: Create the ported test**

`src/tests/FluentValidation.UnitTests/Services/FluentValidationAggregateValidatorTests.cs`:
```csharp
using FluentValidation;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.FluentValidation;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.FluentValidation.Tests;

public sealed class FluentValidationAggregateValidatorTests
{
	[Test]
	public async Task ValidateAsync_UsesAsyncRules()
	{
		var asyncRuleInvoked = false;
		var aggregate = new TestAggregate { Name = "invalid" };
		var validator = new InlineValidator<TestAggregate>();
		validator
			.RuleFor(m => m.Name)
			.MustAsync(
				(_, _) =>
				{
					asyncRuleInvoked = true;
					return Task.FromResult(true);
				}
			);

		var adapter = new FluentValidationAggregateValidator<TestAggregate>(validator);

		var result = await adapter.ValidateAsync(aggregate);

		await Assert.That(asyncRuleInvoked).IsTrue();
		await Assert.That(result.IsValid).IsTrue();
	}

	[Test]
	public async Task Validate_WhenFluentValidationFails_MapsToCoreValidationResult()
	{
		var aggregate = new TestAggregate { Name = "" };
		var validator = new InlineValidator<TestAggregate>();
		validator.RuleFor(m => m.Name).NotEmpty();

		var adapter = new FluentValidationAggregateValidator<TestAggregate>(validator);

		var result = adapter.Validate(aggregate);

		await Assert.That(result.IsValid).IsFalse();
		await Assert.That(result.Errors).Count().IsEqualTo(1);
		await Assert.That(result.Errors[0].PropertyName).IsEqualTo("Name");
	}

	sealed class TestAggregate : AggregateBase
	{
		public string Name { get; set; } = string.Empty;

		protected override void RegisterEvents() { }
	}
}
```

**Step 3: Add to solution**

Run:
```bash
cd src && dotnet sln Purview.EventSourcing.slnx add tests/FluentValidation.UnitTests/FluentValidation.UnitTests.csproj
```

**Step 4: Build and run tests**

Run: `dotnet test src/tests/FluentValidation.UnitTests/FluentValidation.UnitTests.csproj`
Expected: PASS — 2 tests pass

**Step 5: Commit**

```bash
git add src/tests/FluentValidation.UnitTests/ src/Purview.EventSourcing.slnx
git commit -m "test: add FluentValidation adapter unit tests"
```

---

### Task 16: Update integration test fixtures (if needed)

**Objective:** Check if any integration test fixtures or SharedTestingFramework reference FluentValidation types.

**Files:**
- Check: `src/tests/SharedTestingFramework/` (all .cs files)
- Check: `src/tests/SqlServer.IntegrationTests/` (all .cs files)
- Check: `src/tests/MongoDB.IntegrationTests/` (all .cs files)
- Check: `src/tests/AzureStorage.IntegrationTests/` (all .cs files)

**Step 1: Search for FluentValidation references in test projects**

Run:
```bash
grep -r "FluentValidation" src/tests/ --include="*.cs" --include="*.csproj"
```

If results found: update each file to use `Purview.EventSourcing.Validation` types instead. The pattern is the same as Task 11: replace `FluentValidation.Results.ValidationResult` → `ValidationResult`, `FluentValidation.Results.ValidationFailure` → `ValidationFailure`, add `using Purview.EventSourcing.Validation;`.

If no results: skip to Step 2.

**Step 2: Build the entire solution**

Run: `dotnet build src/Purview.EventSourcing.slnx`
Expected: PASS — all projects compile

**Step 3: Run all unit tests**

Run: `dotnet test src/Purview.EventSourcing.slnx --filter "Category!=Integration"`
Expected: PASS

**Step 4: Commit if changes were made**

```bash
git add -A
git commit -m "test: update integration test fixtures for core validation types"
```

---

### Task 17: Update documentation

**Objective:** Update the solution design guide to reflect the decoupled validation architecture.

**Files:**
- Modify: `docs/wiki/Solution-Design-Guide.md` (line 361)

**Step 1: Update the Save-Time Validation section**

Replace line 361:
```markdown
Stores run aggregate validation before persistence. With no custom validator, the current implementation uses `DefaultAggregateValidator<TAggregate>`, which validates standard DataAnnotations such as `[Range]`. Stores can also adapt FluentValidation validators through `IValidator<TAggregate>` and `IAggregateValidator<TAggregate>`.
```

With:
```markdown
Stores run aggregate validation before persistence. With no custom validator, the current implementation uses `DefaultAggregateValidator<TAggregate>`, which validates standard DataAnnotations such as `[Range]`. Store constructors accept `IAggregateValidator<TAggregate>?` — when null, the default DataAnnotations validator is used.

FluentValidation integration is available in the separate `Purview.EventSourcing.FluentValidation` package, which provides `FluentValidationAggregateValidator<TAggregate>` to adapt `FluentValidation.IValidator<T>` to `IAggregateValidator<T>`. Register it via `AddFluentValidationAdapter<TAggregate, TValidator>()` or `AddFluentValidationAdapter<TAggregate>()` DI extensions.
```

**Step 2: Commit**

```bash
git add docs/wiki/Solution-Design-Guide.md
git commit -m "docs: update solution design guide for decoupled validation"
```

---

## Files Changed Summary

### New files
- `src/src/EventSourcing/Validation/ValidationResult.cs`
- `src/src/EventSourcing/Validation/ValidationFailure.cs`
- `src/src/EventSourcing/Validation/ValidationException.cs`
- `src/src/FluentValidation/FluentValidation.csproj`
- `src/src/FluentValidation/Services/FluentValidationAggregateValidator.cs`
- `src/src/FluentValidation/Extensions/Microsoft/Extensions/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/tests/FluentValidation.UnitTests/FluentValidation.UnitTests.csproj`
- `src/tests/FluentValidation.UnitTests/Services/FluentValidationAggregateValidatorTests.cs`

### Deleted files
- `src/src/EventSourcing/Services/FluentValidationAggregateValidator.cs`
- `src/src/EventSourcing/Services/AggregateValidatorAdapter.cs`
- `src/tests/EventSourcing.UnitTests/Services/FluentValidationAggregateValidatorTests.cs`

### Modified files
- `src/src/EventSourcing/EventSourcing.csproj` — remove FluentValidation PackageReference
- `src/src/EventSourcing/Services/IAggregateValidator.cs` — return core ValidationResult
- `src/src/EventSourcing/Services/DefaultAggregateValidator.cs` — no AbstractValidator base
- `src/src/EventSourcing/Aggregates/SaveResult.cs` — use core types
- `src/src/InMemory/Events/InMemoryEventStore.cs` — IAggregateValidator<T>? param
- `src/src/InMemory/Snapshots/InMemorySnapshotStore.cs` — IAggregateValidator<T>? param
- `src/src/SqlServer/Events/SqlServerEventStore.cs` — IAggregateValidator<T>? param
- `src/src/SqlServer/Events/SqlServerEventStore.SaveAsync.cs` — core ValidationResult using
- `src/src/MongoDB/Events/MongoDBEventStore.cs` — IAggregateValidator<T>? param
- `src/src/MongoDB/Events/MongoDBEventStore.SaveAsync.cs` — core ValidationResult using
- `src/src/AzureStorage/TableEventStore.cs` — IAggregateValidator<T>? param
- `src/src/AzureStorage/TableEventStore.SaveAsync.cs` — core ValidationResult using
- `src/src/Samples.QuickStart/Infrastructure/InMemoryTransactionalEventStore.cs` — core ValidationResult using
- `src/tests/EventSourcing.UnitTests/EventStoreTransactionTests.cs` — core ValidationResult
- `src/tests/EventSourcing.UnitTests/IEventStoreExtensionsEnlistTests.cs` — core ValidationResult
- `src/tests/EventSourcing.UnitTests/SqlServer/Snapshots/SqlServerSnapshotEventStoreTests.cs` — core ValidationResult
- `docs/wiki/Solution-Design-Guide.md` — updated validation section
- `src/Purview.EventSourcing.slnx` — add new projects

### Unchanged (kept in Directory.Packages.props)
- `Directory.Packages.props` line 13 — `FluentValidation` version stays (needed by the new package)

---

## Risks, Tradeoffs, and Open Questions

### Breaking changes
This is a **breaking change** for consumers of the `Purview.EventSourcing` NuGet package:
1. `SaveResult<T>.ValidationResult` type changes from `FluentValidation.Results.ValidationResult` to `Purview.EventSourcing.Validation.ValidationResult`
2. Store constructors change from `FluentValidation.IValidator<T>?` to `IAggregateValidator<T>?`
3. `SaveResult<T>.EnsureValid()` throws `Purview.EventSourcing.Validation.ValidationException` instead of `FluentValidation.ValidationException`
4. `FluentValidationAggregateValidator<T>` and `AggregateValidatorAdapter` move to a separate package

This should be accompanied by a major version bump.

### Migration path for consumers
1. If using DataAnnotations only (no FluentValidation): no code changes needed — the default validator behavior is unchanged.
2. If using FluentValidation: add the `Purview.EventSourcing.FluentValidation` package and call `AddFluentValidationAdapter<TAggregate, TValidator>()` in DI.
3. If constructing `SaveResult<T>` directly in tests: change `FluentValidation.Results.ValidationResult` → `Purview.EventSourcing.Validation.ValidationResult`.

### Tradeoff: concrete types vs interfaces for validation results
The plan uses concrete `ValidationResult`/`ValidationFailure` classes rather than interfaces. This keeps the API simple and makes test construction trivial. If a future validation library needs to plug in its own result type, it can construct core `ValidationResult` instances from its own results — the adapter pattern handles this.

### Open question: should the FluentValidation package auto-register for all aggregates?
The current plan uses explicit per-type registration (`AddFluentValidationAdapter<TAggregate, TValidator>()`). An assembly-scanning `AddFluentValidationAdaptersFromAssembly()` convenience method could be added later if demand exists. Kept out of initial scope per YAGNI.

### Open question: nullable IAggregateValidator in DI
When using `AddFluentValidationAdapter<TAggregate>()` (the factory variant), the factory returns `null!` when no `IValidator<T>` is registered. MS DI will register this as a null singleton, which could cause issues. The explicit `AddFluentValidationAdapter<TAggregate, TValidator>()` variant avoids this. Consider documenting that users should use the explicit variant.
