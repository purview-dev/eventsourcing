using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Extensions;

namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static class AggregateEventMethodBuilder
{
	const int HintNameHashHexLength = 16;

	const string GeneratedSourceFileSuffix = ".g.cs";

	static readonly int HintNameSeparatorAndSuffixLength =
		1 + HintNameHashHexLength + GeneratedSourceFileSuffix.Length;

	public static bool TryBuild(
		INamedTypeSymbol classSymbol,
		IMethodSymbol methodSymbol,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		Compilation compilation,
		INamedTypeSymbol? valueObjectContextType,
		string? aggregateNamespace,
		string? aggregateEventNamespaceOverride,
		string? aggregateEventSuffixOverride,
		string? assemblyEventSuffix,
		List<DiagnosticInfo> diagnostics,
		CancellationToken ct,
		out AggregateEventMethodInfo methodInfo
	)
	{
		methodInfo = default!;
		var eventSuffix = (aggregateEventSuffixOverride ?? assemblyEventSuffix ?? "Event").Trim();

		var eventAttribute = EventAttributeData.FromAttributeData(methodSymbol);
		var collectionEventAttribute = CollectionEventAttributeData.FromAttributeData(methodSymbol);

		if (eventAttribute.Exists && collectionEventAttribute.Exists)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.UnsupportedEventMethodSignature,
					methodSymbol,
					methodSymbol.Name,
					$"methods cannot combine [{TypeLibrary.Attributes.EventAttribute.RenderTypeName}] and [{TypeLibrary.Attributes.CollectionEventAttribute.RenderTypeName}]"
				)
			);
			return false;
		}

		if (!methodSymbol.IsPartialDefinition)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventMethodMustBePartial,
					methodSymbol,
					methodSymbol.Name
				)
			);

			return false;
		}

		if (
			!ValidateSignature(
				classSymbol,
				methodSymbol,
				collectionEventAttribute.Exists,
				diagnostics,
				out var returnTypeName,
				out var returnKind
			)
		)
			return false;

		if (collectionEventAttribute.Exists)
		{
			if (collectionEventAttribute.Version < 1)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.EventSchemaVersionMustBePositive,
						methodSymbol,
						methodSymbol.Name,
						methodSymbol.ContainingType.Name,
						collectionEventAttribute.Version
					)
				);

				return false;
			}
		}

		if (
			!BuildParameters(
				methodSymbol,
				classSymbol,
				collectionEventAttribute,
				collectionEventAttribute.Manual,
				propertySymbolsByName,
				compilation,
				valueObjectContextType,
				diagnostics,
				ct,
				out var parameters,
				out var collectionEvent
			)
		)
			return false;

		if (
			!ResolveEventName(
				methodSymbol,
				classSymbol,
				collectionEventAttribute.Exists,
				collectionEvent,
				eventSuffix,
				hasExplicitEventName,
				eventName,
				diagnostics,
				out var resolvedEventName
			)
		)
			return false;

		var eventNamespace = string.IsNullOrWhiteSpace(eventNamespaceOverride)
			? string.IsNullOrWhiteSpace(aggregateEventNamespaceOverride)
				? CreateDefaultEventNamespace(aggregateNamespace, classSymbol.Name)
				: aggregateEventNamespaceOverride!.Trim()
			: eventNamespaceOverride!.Trim();

		methodInfo = new(
			methodSymbol.Name,
			new(resolvedEventName, eventNamespace),
			parameters,
			returnType,
			returnKind,
			methodSymbol.DeclaredAccessibility,
			version,
			hasExplicitVersion,
			collectionEventAttribute.Manual,
			collectionEvent
		);
		return true;
	}

	static bool ValidateSignature(
		INamedTypeSymbol classSymbol,
		IMethodSymbol methodSymbol,
		bool isCollectionEvent,
		List<DiagnosticInfo> diagnostics,
		out TypeReferenceOptions returnType,
		out EventMethodReturnKind returnKind
	)
	{
		returnType = PurviewTypeLibrary.System.Void;
		returnKind = EventMethodReturnKind.Void;
		var hasErrors = false;

		void ReportUnsupportedSignature(string reason)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.UnsupportedEventMethodSignature,
					methodSymbol,
					methodSymbol.Name,
					reason
				)
			);
			hasErrors = true;
		}

		if (
			methodSymbol.DeclaredAccessibility == Accessibility.Public
			&& !EventVerbMap.IsVerbPhrase(methodSymbol.Name)
		)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.AggregateMethodShouldBeVerbPhrase,
					methodSymbol,
					methodSymbol.Name
				)
			);
		}

		if (methodSymbol.IsStatic)
			ReportUnsupportedSignature("static methods are not supported");

		if (methodSymbol.TypeParameters.Length > 0)
			ReportUnsupportedSignature("generic methods are not supported");

		if (!TryResolveReturnKind(methodSymbol, classSymbol, out returnType, out returnKind))
		{
			ReportUnsupportedSignature(
				"methods must return void, bool, or the containing aggregate type"
			);
		}

		if (methodSymbol.PartialImplementationPart is not null)
			ReportUnsupportedSignature("methods must be partial declarations without a body");

		foreach (var parameter in methodSymbol.Parameters)
		{
			if (parameter.RefKind != RefKind.None)
				ReportUnsupportedSignature("ref, in, and out parameters are not supported");

			if (
				parameter.IsParams && (!isCollectionEvent || parameter.Type is not IArrayTypeSymbol)
			)
				ReportUnsupportedSignature("params parameters are not supported");
		}

		if (isCollectionEvent && methodSymbol.Parameters.Length != 1)
			ReportUnsupportedSignature("collection event methods must have exactly one parameter");

		return !hasErrors;
	}

	static bool ResolveAttributeValues(
		IMethodSymbol methodSymbol,
		AttributeData? eventAttribute,
		AttributeData? collectionEventAttribute,
		List<DiagnosticInfo> diagnostics,
		out int version,
		out bool hasExplicitVersion,
		out string eventName,
		out bool hasExplicitEventName,
		out string? eventNamespaceOverride,
		out bool manualApply
	)
	{
		version = 1;
		hasExplicitVersion = false;
		eventName = string.Empty;
		hasExplicitEventName = false;
		eventNamespaceOverride = null;
		manualApply = false;

		var activeAttribute = collectionEventAttribute ?? eventAttribute;
		if (activeAttribute is null)
			return true;

		hasExplicitVersion = activeAttribute.NamedArguments.Any(static arg => arg.Key == "Version");
		if (hasExplicitVersion)
			version = activeAttribute.GetNamedArgument("Version", 1);

		var explicitEventName = activeAttribute.GetNamedArgument<string>("EventName", null);
		if (!string.IsNullOrEmpty(explicitEventName))
		{
			eventName = explicitEventName!.Trim();
			hasExplicitEventName = true;
		}

		eventNamespaceOverride = activeAttribute.GetNamedArgument<string>("EventNamespace", null);

		manualApply = activeAttribute.GetNamedArgument("Manual", false);

		if (version < 1)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventSchemaVersionMustBePositive,
					methodSymbol,
					methodSymbol.Name,
					methodSymbol.ContainingType.Name,
					version
				)
			);
			return false;
		}

		return true;
	}

	static bool BuildParameters(
		IMethodSymbol methodSymbol,
		INamedTypeSymbol classSymbol,
		CollectionEventAttributeData collectionEventAttribute,
		bool manualApply,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		Compilation compilation,
		INamedTypeSymbol? valueObjectContextType,
		List<DiagnosticInfo> diagnostics,
		CancellationToken ct,
		out List<EventPropertyInfo> parameters,
		out CollectionEventInfo? collectionEvent
	)
	{
		parameters = [];
		collectionEvent = null;

		if (collectionEventAttribute.Exists)
		{
			if (
				!TryCreateCollectionEventInfo(
					methodSymbol,
					collectionEventAttribute,
					propertySymbolsByName,
					diagnostics,
					out var collectionParameter,
					out collectionEvent
				)
			)
			{
				return false;
			}

			parameters.Add(collectionParameter);
			return true;
		}

		var hasErrors = false;
		foreach (var parameter in methodSymbol.Parameters)
		{
			ct.ThrowIfCancellationRequested();

			if (
				TryBuildEventPropertyInfo(
					parameter,
					methodSymbol,
					classSymbol,
					manualApply,
					propertySymbolsByName,
					compilation,
					valueObjectContextType,
					diagnostics,
					out var propertyInfo
				)
			)
			{
				parameters.Add(propertyInfo);
			}
			else
			{
				hasErrors = true;
			}
		}

		return !hasErrors;
	}

	static bool TryBuildEventPropertyInfo(
		IParameterSymbol parameter,
		IMethodSymbol methodSymbol,
		INamedTypeSymbol classSymbol,
		bool manualApply,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		Compilation compilation,
		INamedTypeSymbol? valueObjectContextType,
		List<DiagnosticInfo> diagnostics,
		out EventPropertyInfo propertyInfo
	)
	{
		propertyInfo = default!;

		var aggregatePropertyName =
			GetAggregatePropertyNameOverride(parameter)
			?? EventPropertyInfo.ToPropertyName(parameter.Name);
		var isComputedParameter = HasComputedAttribute(parameter);
		var isNotNull = HasNotNullAttribute(parameter);
		var isRequired = HasRequiredAttribute(parameter);
		var isStringParameter = parameter.Type.SpecialType == SpecialType.System_String;
		TypeReferenceOptions parameterType = new(parameter.Type);

		var metadata = MetadataAttributeData.FromAttributeData(parameter);

		if (metadata.Exists)
		{
			if (isComputedParameter)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.EventParameterMustMapToWritableProperty,
						parameter,
						parameter.Name,
						methodSymbol.Name,
						classSymbol.Name,
						"parameter cannot be marked with both [Metadata] and [Computed]"
					)
				);
				return false;
			}

			propertyInfo = new EventPropertyInfo(
				parameter.Name,
				parameterType,
				parameterType,
				aggregatePropertyName,
				false,
				metadata.Store,
				parameterType,
				isStringParameter,
				EventParameterConversionKind.None,
				IsComputed: false,
				IsNotNull: isNotNull,
				IsRequired: isRequired,
				IsString: isStringParameter
			);

			return true;
		}

		if (
			manualApply
			&& (
				!propertySymbolsByName.TryGetValue(
					aggregatePropertyName,
					out var manualMappedProperty
				)
				|| manualMappedProperty.SetMethod is null
				|| manualMappedProperty.SetMethod.IsInitOnly
			)
		)
		{
			propertyInfo = new EventPropertyInfo(
				parameter.Name,
				parameterType,
				parameterType,
				aggregatePropertyName,
				false,
				true,
				parameterType,
				isStringParameter,
				EventParameterConversionKind.None,
				IsComputed: isComputedParameter,
				IsNotNull: isNotNull,
				IsRequired: isRequired,
				IsString: isStringParameter
			);
			return true;
		}

		if (!propertySymbolsByName.TryGetValue(aggregatePropertyName, out var propertySymbol))
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventParameterMustMapToWritableProperty,
					parameter,
					parameter.Name,
					methodSymbol.Name,
					classSymbol.Name,
					$"property '{aggregatePropertyName}' does not exist"
				)
			);
			return false;
		}

		if (propertySymbol.SetMethod is null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventParameterMustMapToWritableProperty,
					parameter,
					parameter.Name,
					methodSymbol.Name,
					classSymbol.Name,
					$"property '{aggregatePropertyName}' does not have a setter"
				)
			);
			return false;
		}

		if (propertySymbol.SetMethod.IsInitOnly)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventParameterMustMapToWritableProperty,
					parameter,
					parameter.Name,
					methodSymbol.Name,
					classSymbol.Name,
					$"property '{aggregatePropertyName}' is init-only"
				)
			);
			return false;
		}

		TypeReferenceOptions propertyType = new(propertySymbol.Type);
		var conversionKind = ResolveParameterConversionKind(
			compilation,
			classSymbol,
			parameter.Type,
			propertySymbol.Type,
			valueObjectContextType
		);
		if (conversionKind is null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventParameterMustMapToWritableProperty,
					parameter,
					parameter.Name,
					methodSymbol.Name,
					classSymbol.Name,
					$"parameter type '{parameterType}' cannot be mapped to property '{aggregatePropertyName}' of type '{propertyTypeName}' via implicit conversion or value-object Create(...)"
				)
			);
			return false;
		}

		if (
			conversionKind == EventParameterConversionKind.Implicit
			&& SymbolEqualityComparer.Default.Equals(parameter.Type, propertySymbol.Type)
			&& propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated
			&& parameter.Type.NullableAnnotation != NullableAnnotation.Annotated
		)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventParameterNullabilityMismatch,
					parameter,
					parameter.Name,
					methodSymbol.Name,
					aggregatePropertyName,
					propertyType
				)
			);
		}

		propertyInfo = new EventPropertyInfo(
			parameter.Name,
			parameterType,
			propertyType,
			propertySymbol.Name,
			true,
			true,
			propertyType,
			parameter.Type.SpecialType == SpecialType.System_String
				&& propertySymbol.Type.SpecialType == SpecialType.System_String,
			conversionKind.Value,
			IsComputed: isComputedParameter,
			IsNotNull: isNotNull,
			IsRequired: isRequired,
			IsString: isStringParameter
		);
		return true;
	}

	static bool ResolveEventName(
		IMethodSymbol methodSymbol,
		INamedTypeSymbol classSymbol,
		bool isCollectionEvent,
		CollectionEventInfo? collectionEvent,
		string eventSuffix,
		bool hasExplicitEventName,
		string eventName,
		List<DiagnosticInfo> diagnostics,
		out string resolvedEventName
	)
	{
		resolvedEventName = eventName;

		if (hasExplicitEventName)
		{
			if (!EventVerbMap.IsPastTenseEventName(resolvedEventName))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.EventNameOverrideShouldBePastTense,
						methodSymbol,
						resolvedEventName,
						methodSymbol.Name
					)
				);
			}
		}
		else if (
			!EventVerbMap.TryCreateGeneratedEventName(
				methodSymbol.Name,
				classSymbol.Name,
				out resolvedEventName
			)
		)
		{
			var suggestedMethodName = EventVerbMap.TrySuggestVerbPhrase(
				methodSymbol.Name,
				out var suggestedVerbPhrase
			)
				? suggestedVerbPhrase
				: $"Create{TrimAggregateSuffix(classSymbol.Name)}";

			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.UnableToInferEventName,
					methodSymbol,
					methodSymbol.Name,
					suggestedMethodName
				)
			);
			return false;
		}
		else
		{
			if (
				isCollectionEvent
				&& collectionEvent is not null
				&& collectionEvent.ParameterShape == CollectionParameterShape.Array
			)
				resolvedEventName += "Array";

			resolvedEventName += eventSuffix;
		}

		return true;
	}

	public static bool IsCollectionLikeType(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is IArrayTypeSymbol)
			return true;

		if (typeSymbol.SpecialType == SpecialType.System_String)
			return false;

		if (typeSymbol is not INamedTypeSymbol namedType)
			return false;

		if (
			namedType.IsGenericType
			&& namedType.OriginalDefinition.SpecialType
				== SpecialType.System_Collections_Generic_IEnumerable_T
		)
			return true;

		foreach (var interfaceSymbol in namedType.AllInterfaces)
		{
			if (
				interfaceSymbol is INamedTypeSymbol namedInterface
				&& namedInterface.IsGenericType
				&& namedInterface.OriginalDefinition.SpecialType
					== SpecialType.System_Collections_Generic_IEnumerable_T
			)
				return true;
		}

		return false;
	}

	public static bool TryGetCollectionDetails(
		ITypeSymbol typeSymbol,
		out ITypeSymbol elementType,
		out bool isSet
	)
	{
		elementType = null!;
		isSet = false;

		if (typeSymbol is not INamedTypeSymbol namedType || !namedType.IsGenericType)
			return false;

		if (TypeLibrary.Aggregates.EventStoreList.Equals(namedType.OriginalDefinition))
		{
			elementType = namedType.TypeArguments[0];
			isSet = false;
			return true;
		}

		if (TypeLibrary.Aggregates.EventStoreSet.Equals(namedType.OriginalDefinition))
		{
			elementType = namedType.TypeArguments[0];
			isSet = true;
			return true;
		}

		return false;
	}

	public static bool TryGetIEnumerableElementType(
		ITypeSymbol typeSymbol,
		out ITypeSymbol elementType
	)
	{
		elementType = null!;

		if (
			typeSymbol is INamedTypeSymbol namedType
			&& namedType.IsGenericType
			&& namedType.OriginalDefinition.SpecialType
				== SpecialType.System_Collections_Generic_IEnumerable_T
		)
		{
			elementType = namedType.TypeArguments[0];
			return true;
		}

		if (typeSymbol is not INamedTypeSymbol interfaceCarrier)
			return false;

		foreach (var interfaceSymbol in interfaceCarrier.AllInterfaces)
		{
			if (
				interfaceSymbol is INamedTypeSymbol enumerableInterface
				&& enumerableInterface.IsGenericType
				&& enumerableInterface.OriginalDefinition.SpecialType
					== SpecialType.System_Collections_Generic_IEnumerable_T
			)
			{
				elementType = enumerableInterface.TypeArguments[0];
				return true;
			}
		}

		return false;
	}

	public static bool IsEventStoreCollectionType(ITypeSymbol typeSymbol) =>
		typeSymbol is INamedTypeSymbol namedType
		&& namedType.IsGenericType
		&& (
			TypeLibrary.Aggregates.EventStoreList.Equals(namedType.OriginalDefinition)
			|| TypeLibrary.Aggregates.EventStoreSet.Equals(namedType.OriginalDefinition)
		);

	public static bool TryGetComplexScalarValueType(
		ITypeSymbol typeSymbol,
		out string valueTypeDisplayName
	)
	{
		valueTypeDisplayName = string.Empty;

		if (typeSymbol is not INamedTypeSymbol namedType)
			return false;

		if (!HasAttribute(namedType, TypeLibrary.Attributes.ScalarAttribute))
			return false;

		var valueProperty = namedType
			.GetMembers("Value")
			.OfType<IPropertySymbol>()
			.FirstOrDefault(static property =>
				property.GetMethod is not null && !property.IsStatic
			);

		if (valueProperty is null)
			return false;

		if (IsSimpleQueryScalarType(valueProperty.Type))
			return false;

		valueTypeDisplayName = valueProperty.Type.ToDisplayString(
			SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
				SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
					| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
			)
		);
		return true;
	}

	public static bool IsSimpleQueryScalarType(ITypeSymbol typeSymbol)
	{
		if (typeSymbol.TypeKind == TypeKind.Enum)
			return true;

		if (typeSymbol.SpecialType is not SpecialType.None)
			return true;

		// Check for common system types that are often used as scalar values in queries.
		return TypeLibrary.System.Guid.Equals(typeSymbol)
			|| TypeLibrary.System.Uri.Equals(typeSymbol)
			|| TypeLibrary.System.DateTime.Equals(typeSymbol)
			|| TypeLibrary.System.DateTimeOffset.Equals(typeSymbol)
			|| TypeLibrary.System.TimeSpan.Equals(typeSymbol)
			|| TypeLibrary.System.DateOnly.Equals(typeSymbol)
			|| TypeLibrary.System.TimeOnly.Equals(typeSymbol);
	}

	public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeSymbol)
	{
		if (attributeSymbol is null)
			return false;

		foreach (var attribute in symbol.GetAttributes())
		{
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
				return true;
		}

		return false;
	}

	public static bool HasAttribute(ISymbol symbol, TypeValueObject attributeType) =>
		TypeHelpers.HasAttribute(symbol, attributeType.MetadataFullName);

	public static bool HasComputedAttribute(IParameterSymbol parameterSymbol)
	{
		foreach (var attribute in parameterSymbol.GetAttributes())
		{
			var attributeClass = attribute.AttributeClass;
			if (
				attributeClass is not null
				&& TypeLibrary.Attributes.ComputedAttribute.Equals(attributeClass)
			)
				return true;
		}

		return false;
	}

	public static bool HasNotNullAttribute(IParameterSymbol parameterSymbol)
	{
		foreach (var attribute in parameterSymbol.GetAttributes())
		{
			var attributeClass = attribute.AttributeClass;
			if (
				attributeClass is not null
				&& attributeClass.ToDisplayString()
					== "System.Diagnostics.CodeAnalysis.NotNullAttribute"
			)
				return true;
		}

		return false;
	}

	public static bool HasRequiredAttribute(IParameterSymbol parameterSymbol)
	{
		foreach (var attribute in parameterSymbol.GetAttributes())
		{
			var attributeClass = attribute.AttributeClass;
			if (
				attributeClass is not null
				&& attributeClass.ToDisplayString()
					== "System.ComponentModel.DataAnnotations.RequiredAttribute"
			)
				return true;
		}

		return false;
	}

	public static bool IsEventType(INamedTypeSymbol typeSymbol)
	{
		return TypeHelpers.InheritsFrom(typeSymbol, TypeLibrary.Aggregates.EventBase)
			|| TypeHelpers.Implements(typeSymbol, TypeLibrary.Aggregates.IEvent);
	}

	public static string? GetAggregatePropertyNameOverride(IParameterSymbol parameterSymbol)
	{
		foreach (var attribute in parameterSymbol.GetAttributes())
		{
			var attributeClass = attribute.AttributeClass;
			if (
				attributeClass is null
				|| !TypeLibrary.Attributes.PropertyAttribute.Equals(attributeClass)
			)
				continue;

			if (
				attribute.ConstructorArguments.Length == 1
				&& attribute.ConstructorArguments[0].Value is string value
			)
				return value.Trim();

			break;
		}

		return null;
	}

	public static bool HasRegisterEventsMethod(
		INamedTypeSymbol classSymbol,
		out IMethodSymbol? registerEventsMethod
	)
	{
		registerEventsMethod = classSymbol
			.GetMembers("RegisterEvents")
			.OfType<IMethodSymbol>()
			.FirstOrDefault(method =>
				method.Parameters.Length == 0
				&& method.MethodKind == MethodKind.Ordinary
				&& !method.IsImplicitlyDeclared
			);

		return registerEventsMethod is not null;
	}

	public static bool TryResolveReturnKind(
		IMethodSymbol methodSymbol,
		INamedTypeSymbol classSymbol,
		out TypeReferenceOptions returnType,
		out EventMethodReturnKind returnKind
	)
	{
		returnType = PurviewTypeLibrary.System.Void;
		returnKind = EventMethodReturnKind.Void;

		if (methodSymbol.ReturnsVoid)
			return true;

		if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean)
		{
			returnType = PurviewTypeLibrary.System.Boolean;
			returnKind = EventMethodReturnKind.Bool;
			return true;
		}

		if (SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, classSymbol))
		{
			returnType = new(classSymbol);
			returnKind = EventMethodReturnKind.Aggregate;
			return true;
		}

		return false;
	}

	public static bool TryCreateInvalidMethodStub(
		IMethodSymbol methodSymbol,
		string[] diagnosticIds,
		out InvalidAggregateEventMethodInfo methodInfo,
		CancellationToken ct
	)
	{
		ct.ThrowIfCancellationRequested();
		methodInfo = null!;

		var declaration = methodSymbol
			.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(ct))
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(static syntax =>
				syntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword))
				&& syntax.Body is null
				&& syntax.ExpressionBody is null
			);

		if (declaration is null)
			return false;

		var modifiers = string.Join(
			" ",
			declaration.Modifiers.Select(static modifier => modifier.Text)
		);
		if (modifiers.Length > 0)
			modifiers += " ";

		var explicitInterfaceSpecifier =
			declaration.ExplicitInterfaceSpecifier?.ToString() ?? string.Empty;
		var typeParameterList = declaration.TypeParameterList?.ToString() ?? string.Empty;
		var constraints =
			declaration.ConstraintClauses.Count == 0
				? string.Empty
				: " "
					+ string.Join(
						" ",
						declaration.ConstraintClauses.Select(static clause => clause.ToString())
					);

		methodInfo = new InvalidAggregateEventMethodInfo(
			$"{modifiers}{declaration.ReturnType} {explicitInterfaceSpecifier}{declaration.Identifier}{typeParameterList}{declaration.ParameterList}{constraints}",
			diagnosticIds
		);
		return true;
	}

	public static string CreateHintName(INamedTypeSymbol classSymbol)
	{
		var symbolName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var shortName = classSymbol.Name;
		var builder = new System.Text.StringBuilder(
			shortName.Length + HintNameSeparatorAndSuffixLength
		);

		foreach (var character in shortName)
		{
			builder.Append(char.IsLetterOrDigit(character) ? character : '_');
		}

		builder.Append('_');
		builder.Append(
			ComputeStableHash(symbolName)
				.ToString($"X{HintNameHashHexLength}", CultureInfo.InvariantCulture)
		);
		builder.Append(GeneratedSourceFileSuffix);
		return builder.ToString();

		static ulong ComputeStableHash(string value)
		{
			const ulong offsetBasis = 14695981039346656037;
			const ulong prime = 1099511628211;

			var hash = offsetBasis;
			foreach (var character in value)
			{
				hash ^= character;
				hash *= prime;
			}

			return hash;
		}
	}

	static EventParameterConversionKind? ResolveParameterConversionKind(
		Compilation compilation,
		INamedTypeSymbol aggregateType,
		ITypeSymbol parameterType,
		ITypeSymbol propertyType,
		INamedTypeSymbol? valueObjectContextType
	)
	{
		if (SymbolEqualityComparer.Default.Equals(parameterType, propertyType))
		{
			// Same underlying type. If nullability differs (non-nullable param → nullable property),
			// return Implicit so a typed local variable is generated and ref-hook calls don't produce
			// CS8600 ("Converting null literal or possible null value to non-nullable type").
			return
				parameterType.NullableAnnotation != propertyType.NullableAnnotation
				&& propertyType.NullableAnnotation == NullableAnnotation.Annotated
				? EventParameterConversionKind.Implicit
				: EventParameterConversionKind.None;
		}

		if (
			propertyType is INamedTypeSymbol namedPropertyType
			&& TryResolveValueObjectCreateConversion(
				aggregateType,
				namedPropertyType,
				parameterType,
				valueObjectContextType,
				out var createConversionKind
			)
		)
		{
			return createConversionKind;
		}

		var conversion = compilation.ClassifyConversion(parameterType, propertyType);
		return conversion.Exists && conversion.IsImplicit
			? EventParameterConversionKind.Implicit
			: null;
	}

	static bool TryResolveValueObjectCreateConversion(
		INamedTypeSymbol aggregateType,
		INamedTypeSymbol propertyType,
		ITypeSymbol parameterType,
		INamedTypeSymbol? contextTypeDefinition,
		out EventParameterConversionKind conversionKind
	)
	{
		conversionKind = EventParameterConversionKind.None;

		var hasScalarAttribute = HasAttribute(propertyType, TypeLibrary.Attributes.ScalarAttribute);
		var createMethods = propertyType.GetMembers("Create").OfType<IMethodSymbol>().ToArray();

		var hasContextualCreate = createMethods.Any(method =>
			IsContextualCreateMethod(
				method,
				propertyType,
				aggregateType,
				parameterType,
				contextTypeDefinition
			)
		);

		if (hasContextualCreate)
		{
			conversionKind = EventParameterConversionKind.ContextualCreate;
			return true;
		}

		var hasSimpleCreate = createMethods.Any(method =>
			IsSimpleCreateMethod(method, propertyType, parameterType)
		);

		if (hasSimpleCreate || hasScalarAttribute)
		{
			conversionKind = EventParameterConversionKind.Create;
			return true;
		}

		return false;
	}

	static bool IsSimpleCreateMethod(
		IMethodSymbol method,
		ITypeSymbol returnType,
		ITypeSymbol parameterType
	)
	{
		return method.IsStatic
			&& method.DeclaredAccessibility == Accessibility.Public
			&& method.Name == "Create"
			&& method.Parameters.Length == 1
			&& SymbolEqualityComparer.Default.Equals(method.ReturnType, returnType)
			&& SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, parameterType);
	}

	static bool IsContextualCreateMethod(
		IMethodSymbol method,
		ITypeSymbol returnType,
		INamedTypeSymbol aggregateType,
		ITypeSymbol parameterType,
		INamedTypeSymbol? contextTypeDefinition
	)
	{
		if (
			!method.IsStatic
			|| method.DeclaredAccessibility != Accessibility.Public
			|| method.Name != "Create"
		)
			return false;

		if (method.Parameters.Length != 2)
			return false;

		if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, returnType))
			return false;

		if (!SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, parameterType))
			return false;

		var contextParameter = method.Parameters[1];
		return contextParameter.RefKind == RefKind.In
			&& contextTypeDefinition is not null
			&& contextParameter.Type is INamedTypeSymbol contextType
			&& SymbolEqualityComparer.Default.Equals(
				contextType.OriginalDefinition,
				contextTypeDefinition
			)
			&& contextType.TypeArguments.Length == 1
			&& SymbolEqualityComparer.Default.Equals(contextType.TypeArguments[0], aggregateType);
	}

	static string TrimAggregateSuffix(string aggregateClassName) =>
		aggregateClassName.EndsWith("Aggregate", StringComparison.Ordinal)
			? aggregateClassName.Substring(0, aggregateClassName.Length - "Aggregate".Length)
			: aggregateClassName;

	static string CreateDefaultEventNamespace(string? aggregateNamespace, string aggregateClassName)
	{
		var aggregateNameWithoutSuffix = aggregateClassName;
		if (aggregateClassName.EndsWith("Aggregate", StringComparison.Ordinal))
		{
			aggregateNameWithoutSuffix = aggregateClassName.Substring(
				0,
				aggregateClassName.Length - "Aggregate".Length
			);
		}

		if (string.IsNullOrEmpty(aggregateNameWithoutSuffix))
			aggregateNameWithoutSuffix = aggregateClassName;

		return string.IsNullOrEmpty(aggregateNamespace)
			? aggregateNameWithoutSuffix
			: $"{aggregateNamespace}.{aggregateNameWithoutSuffix}Events";
	}

	static bool TryCreateCollectionEventInfo(
		IMethodSymbol methodSymbol,
		CollectionEventAttributeData collectionEventAttribute,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		List<DiagnosticInfo> diagnostics,
		out EventPropertyInfo parameterInfo,
		out CollectionEventInfo? collectionEvent
	)
	{
		parameterInfo = default!;
		collectionEvent = null;

		var methodLocation = methodSymbol.Locations.FirstOrDefault();
		if (methodSymbol.Parameters.Length != 1)
			return false;

		if (string.IsNullOrWhiteSpace(collectionEventAttribute.PropertyName))
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.UnsupportedEventMethodSignature,
					methodLocation,
					methodSymbol.Name,
					"collection property name must be provided via [CollectionEvent(nameof(CollectionProperty))]"
				)
			);

			return false;
		}

		if (
			!propertySymbolsByName.TryGetValue(
				collectionEventAttribute.PropertyName,
				out var collectionProperty
			)
		)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventParameterMustMapToWritableProperty,
					methodLocation,
					methodSymbol.Parameters[0].Name,
					methodSymbol.Name,
					methodSymbol.ContainingType.Name,
					$"collection property '{collectionEventAttribute.PropertyName}' does not exist"
				)
			);

			return false;
		}

		if (!TryGetCollectionDetails(collectionProperty.Type, out var elementType, out var isSet))
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventParameterMustMapToWritableProperty,
					methodLocation,
					methodSymbol.Parameters[0].Name,
					methodSymbol.Name,
					methodSymbol.ContainingType.Name,
					$"collection property '{collectionEventAttribute.PropertyName}' must use {TypeLibrary.Aggregates.EventStoreList.MakeGeneric("T")} or {TypeLibrary.Aggregates.EventStoreSet.MakeGeneric("T")}"
				)
			);
			return false;
		}

		var parameter = methodSymbol.Parameters[0];

		TypeReferenceOptions parameterType = new(elementType);
		TypeReferenceOptions eventPropertyType;
		CollectionParameterShape parameterShape;
		if (TypeHelpers.IsArray(parameter.Type))
		{
			parameterShape = CollectionParameterShape.Array;
			eventPropertyType = parameterType.MakeArray();
		}
		else if (TryGetIEnumerableElementType(parameter.Type, out var enumerableElementType))
		{
			if (!SymbolEqualityComparer.Default.Equals(enumerableElementType, elementType))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.UnsupportedEventMethodSignature,
						parameter,
						methodSymbol.Name,
						$"collection item type '{parameter.Type}' does not match '{parameterType}'"
					)
				);
				return false;
			}

			parameterShape = CollectionParameterShape.Enumerable;
			eventPropertyType = parameterType.MakeArray();
		}
		else
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.UnsupportedEventMethodSignature,
					parameter.Locations.FirstOrDefault() ?? methodLocation,
					methodSymbol.Name,
					$"collection methods only support '{parameterType}', '{parameterType.MakeArray()}', or IEnumerable<{parameterType}> parameters"
				)
			);
			return false;
		}

		parameterInfo = new EventPropertyInfo(
			parameter.Name,
			parameterType,
			eventPropertyType,
			"ON CRAP LOST THIS SOMEWHERE IN THE REFACTOR",
			HasAggregateProperty: false,
			IncludeInEvent: true,
			EqualityComparerTypeName: eventPropertyType,
			UseStringOrdinalComparison: false,
			ParameterConversionKind: EventParameterConversionKind.None,
			IsComputed: false,
			IsParams: parameter.IsParams
		);

		collectionEvent = new CollectionEventInfo(
			collectionProperty.Name,
			parameterType,
			new(collectionProperty.Type),
			isSet,
			(CollectionMutationOperation)
				Enum.Parse(typeof(CollectionMutationOperation), collectionEventAttribute.Operation),
			parameterShape,
			methodSymbol.Name
		);

		return true;
	}
}
