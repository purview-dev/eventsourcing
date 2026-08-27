using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.EntityFrameworkCore.SourceGenerator.Heleprs;

static class DiagnosticLibrary
{
	public static readonly DiagnosticDescriptor UnsupportedDictionary = new(
		"EVENTSTOREEF001",
		"Dictionary-like property cannot be mapped as an EF complex property",
		"Property '{0}' has dictionary-like type '{1}', which cannot be structurally mapped in an EF snapshot. Mark it [EfOpaque] to persist it as non-queryable JSON, or replace it with a collection of '{0}Entry' complex objects containing key and value members.",
		"EntityFrameworkCore",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: WellKnownDiagnosticTags.CompilationEnd
	);

	public static readonly DiagnosticDescriptor OpaqueQuery = new(
		"EVENTSTOREEF002",
		"Opaque EF snapshot property cannot be queried",
		"Property '{0}' is marked [EFOpaque] and is persisted as opaque JSON; it cannot be referenced in an EF snapshot query expression. Add a separately mapped query property when filtering or ordering is required.",
		"EntityFrameworkCore",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	// Leave this at the bottom...!
	public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [UnsupportedDictionary, OpaqueQuery];
}
