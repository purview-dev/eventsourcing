using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

sealed record class AggregateInfo(
	TypeReferenceOptions AggregateClass,
	Accessibility Accessibility,
	bool ShouldDeclareAggregateBase,
	List<AggregateStatePropertyInfo> Properties,
	List<AggregateEventMethodInfo> Methods,
	List<InvalidAggregateEventMethodInfo> InvalidMethods,
	string HintName
);

sealed record class AggregateStatePropertyInfo(
	string PropertyName,
	TypeReferenceOptions PropertyType
);

/// <summary>
///
/// </summary>
/// <param name="MethodName"></param>
/// <param name="EventType"></param>
/// <param name="Parameters"></param>
/// <param name="ReturnType"></param>
/// <param name="ReturnKind"></param>
/// <param name="MethodAccessibility"></param>
/// <param name="Version">
/// The schema version declared via <c>[Event(Version = N)]</c>.
/// Defaults to 1.
/// </param>
/// <param name="IsSchemaVersionExplicit">Indicates whether <see cref="Version"/> was explicitly configured on the event attribute.</param>
/// <param name="ManualApply">Indicates whether Apply(...) implementation is user-supplied and should not be auto-generated.</param>
/// <param name="CollectionEvent">Collection-event metadata when the method is decorated with [CollectionEvent].</param>
sealed record class AggregateEventMethodInfo(
	string MethodName,
	TypeReferenceOptions EventType,
	List<EventPropertyInfo> Parameters,
	TypeReferenceOptions ReturnType,
	EventMethodReturnKind ReturnKind,
	Accessibility MethodAccessibility,
	int Version = 1,
	bool IsSchemaVersionExplicit = false,
	bool ManualApply = false,
	CollectionEventInfo? CollectionEvent = null
)
{
	public bool IsCollectionEvent => CollectionEvent is not null;
}

sealed record class CollectionEventInfo(
	string PropertyName,
	TypeReferenceOptions ElementType,
	TypeReferenceOptions PropertyType,
	bool IsSet,
	CollectionMutationOperation Operation,
	CollectionParameterShape ParameterShape,
	string NormalizeValidateHookSuffix
);

enum CollectionParameterShape
{
	Single = 0,
	Enumerable = 1,
	Array = 2,
}

enum CollectionMutationOperation
{
	Add = 0,

	Remove = 1,
}

sealed record class InvalidAggregateEventMethodInfo(string Signature, string[] DiagnosticIds);

/// <param name="ParameterName"></param>
/// <param name="ParameterType"></param>
/// <param name="PropertyType"></param>
/// <param name="AggregatePropertyName"></param>
/// <param name="HasAggregateProperty"></param>
/// <param name="IncludeInEvent"></param>
/// <param name="EqualityComparerTypeName"></param>
/// <param name="UseStringOrdinalComparison"></param>
/// <param name="ParameterConversionKind"></param>
/// <param name="IsComputed">
/// Indicates this property is marked with [Computed] attribute.
/// Computed properties are not passed by the caller; instead, they are
/// computed via OnComputingXxxEvent hook before event creation.
/// </param>
/// <param name="IsParams"></param>
/// <param name="IsNotNull"></param>
/// <param name="IsRequired"></param>
/// <param name="IsString"></param>
sealed record class EventPropertyInfo(
	string ParameterName,
	TypeReferenceOptions ParameterType,
	TypeReferenceOptions PropertyType,
	string AggregatePropertyName,
	bool HasAggregateProperty,
	bool IncludeInEvent,
	string EqualityComparerTypeName,
	bool UseStringOrdinalComparison,
	EventParameterConversionKind ParameterConversionKind,
	bool IsComputed = false,
	bool IsParams = false,
	bool IsNotNull = false,
	bool IsRequired = false,
	bool IsString = false
)
{
	public string PropertyName => ToPropertyName(ParameterName);

	public bool RequiresParameterToPropertyTypeConversion =>
		ParameterConversionKind is not EventParameterConversionKind.None;

	public bool RequiresLocalCopy =>
		IsComputed
		|| ParameterConversionKind is not EventParameterConversionKind.None
		|| (IsNotNull && PropertyType.IsNullable)
		|| (IsRequired && (PropertyType.IsNullable || IsString));

	public static string ToPropertyName(string parameterName) =>
		string.IsNullOrEmpty(parameterName)
			? parameterName
			: char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);
}

enum EventParameterConversionKind
{
	None = 0,
	Implicit = 1,
	Create = 2,
	ContextualCreate = 3,
}

sealed record class EventMethodValidationResult(ImmutableArray<DiagnosticInfo> Diagnostics);

sealed record class EventTypeValidationResult(ImmutableArray<DiagnosticInfo> Diagnostics);

enum EventMethodReturnKind
{
	Void = 0,
	Aggregate = 1,
	Bool = 2,
}
