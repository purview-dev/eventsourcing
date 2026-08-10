using Microsoft.CodeAnalysis;

using System.Collections.Immutable;

namespace Purview.EventSourcing.SourceGenerator;

sealed record class AggregateInfo(
	TypeValueObject AggregateClass,
	Accessibility Accessibility,
	bool ShouldDeclareAggregateBase,
	List<AggregateStatePropertyInfo> Properties,
	List<AggregateEventMethodInfo> Methods,
	List<InvalidAggregateEventMethodInfo> InvalidMethods,
	string HintName
);

sealed record class AggregateStatePropertyInfo(string PropertyName, string TypeName);

/// <summary>
/// 
/// </summary>
/// <param name="MethodName"></param>
/// <param name="EventName"></param>
/// <param name="EventNamespace"></param>
/// <param name="Parameters"></param>
/// <param name="ReturnTypeName"></param>
/// <param name="ReturnKind"></param>
/// <param name="MethodAccessibility"></param>
/// <param name="Version">
/// The schema version declared via <c>[GenerateEvent(Version = N)]</c>.
/// Defaults to 1.
/// </param>
/// <param name="IsSchemaVersionExplicit">Indicates whether <see cref="Version"/> was explicitly configured on the event attribute.</param>
/// <param name="ManualApply">Indicates whether Apply(...) implementation is user-supplied and should not be auto-generated.</param>
/// <param name="CollectionEvent">Collection-event metadata when the method is decorated with [GenerateAggregateCollectionEvent].</param>
sealed record class AggregateEventMethodInfo(
	string MethodName,
	string EventName,
	string EventNamespace,
	List<EventPropertyInfo> Parameters,
	string ReturnTypeName,
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
	string ElementTypeName,
	string PropertyTypeName,
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

/// <summary>
/// 
/// </summary>
/// <param name="ParameterName"></param>
/// <param name="ParameterTypeName"></param>
/// <param name="PropertyTypeName"></param>
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
sealed record class EventPropertyInfo(
	string ParameterName,
	string ParameterTypeName,
	string PropertyTypeName,
	string AggregatePropertyName,
	bool HasAggregateProperty,
	bool IncludeInEvent,
	string EqualityComparerTypeName,
	bool UseStringOrdinalComparison,
	EventParameterConversionKind ParameterConversionKind,
	bool IsComputed = false,
	bool IsParams = false
)
{
	public string PropertyName => ToPropertyName(ParameterName);

	public bool RequiresParameterToPropertyTypeConversion =>
		ParameterConversionKind is not EventParameterConversionKind.None;

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

sealed record class EventMethodValidationResult(ImmutableArray<Diagnostic> Diagnostics);

sealed record class EventTypeValidationResult(ImmutableArray<Diagnostic> Diagnostics);

readonly record struct AttributeStringValue(string? Value, bool IsPresent);

enum EventMethodReturnKind
{
	Void = 0,
	Aggregate = 1,
	Bool = 2,
}
