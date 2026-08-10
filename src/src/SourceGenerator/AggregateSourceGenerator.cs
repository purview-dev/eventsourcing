using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using Purview.EventSourcing.SourceGenerator.Extensions.Purview.SourceGeneratorFramework.Helpers;
using Purview.EventSourcing.SourceGenerator.Helpers;

using System.Collections.Immutable;

namespace Purview.EventSourcing.SourceGenerator;

[Generator]
public sealed partial class AggregateSourceGenerator : IIncrementalGenerator
{
	//const string GenerateAggregateAttributeName = "Purview.EventSourcing.Aggregates.GenerateAggregateAttribute";
	//const string GenerateAggregateDefaultsAttributeName =
	//	"Purview.EventSourcing.Aggregates.GenerateAggregateDefaultsAttribute";
	//const string GenerateEventAttributeName =
	//	"Purview.EventSourcing.Aggregates.GenerateEventAttribute";
	//const string GenerateCollectionEventAttributeName =
	//	"Purview.EventSourcing.Aggregates.GenerateCollectionEventAttribute";
	//const string AggregatePropertyAttributeMetadataName = "Purview.EventSourcing.Aggregates.AggregatePropertyAttribute";
	//const string MetadataAttributeMetadataName = "Purview.EventSourcing.Aggregates.MetadataAttribute";
	//const string ComputedAttributeMetadataName = "Purview.EventSourcing.Aggregates.ComputedAttribute";
	//const string EventBaseMetadataName = "Purview.EventSourcing.Aggregates.Events.EventBase";
	//const string IEventMetadataName = "Purview.EventSourcing.Aggregates.Events.IEvent";
	//const string ScalarAttributeMetadataName = "Purview.EventSourcing.Serialization.ScalarAttribute";
	//const string ValueObjectContextMetadataName = "Purview.EventSourcing.ValueObjects.ValueObjectContext`1";
	//const string EventStoreListMetadataName = "Purview.EventSourcing.EventStoreList<T>";
	//const string EventStoreSetMetadataName = "Purview.EventSourcing.EventStoreSet<T>";
	//const int HintNameHashHexLength = 16;
	//const string GeneratedSourceFileSuffix = ".g.cs";

	//static readonly int HintNameSeparatorAndSuffixLength = 1 + HintNameHashHexLength + GeneratedSourceFileSuffix.Length;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx =>
		{
			_logger?.Debug("Adding attribute definitions to compilation");

			ctx.AddEmbeddedAttributeDefinition();
			_logger?.Debug($" - EmbeddedAttribute");

			foreach (var attribute in TypeLibrary.Attributes.GeneratedAttributes)
			{
				_logger?.Debug($" - {attribute.TypeName}");

				ctx.AddSource(
					$"{attribute.TypeName}.g.cs",
					EmbeddedResources.Load(attribute.TypeName)
				);
			}
		});

		//// Opt-out: set <DisableEventSourcingSourceGenerator>true</DisableEventSourcingSourceGenerator> to skip generation.
		//var isDisabled = context.AnalyzerConfigOptionsProvider.Select(
		//	(opts, _) =>
		//	{
		//		opts.GlobalOptions.TryGetValue("build_property.DisableEventSourcingSourceGenerator", out var val);
		//		var isDisabled = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);

		//		if (isDisabled)
		//			_logger?.Debug("EventSourcingSourceGenerator is disabled via MSBuild property");

		//		return isDisabled;
		//	}
		//);

		// Find all class declarations decorated with [GenerateAggregate]
		var aggregateClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
			GenerateAggregateAttributeName,
			predicate: static (node, _) => node is ClassDeclarationSyntax,
			transform: static (ctx, ct) => GetAggregateGenerationResult(ctx, ct)
		);

		var standaloneEventMethods = context.SyntaxProvider.ForAttributeWithMetadataName(
			GenerateEventAttributeName,
			predicate: static (node, _) => node is MethodDeclarationSyntax,
			transform: static (ctx, _) => GetStandaloneEventMethodValidationResult(ctx)
		);

		var manualEventTypes = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) =>
				node is TypeDeclarationSyntax typeDeclaration && typeDeclaration.BaseList is not null,
			transform: static (ctx, _) => GetEventTypeValidationResult(ctx)
		);

		var computedParameterInvocations = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) => node is InvocationExpressionSyntax,
			transform: static (ctx, ct) => GetComputedParameterInvocationValidationResult(ctx, ct)
		);

		var nullableScalarNullComparisons = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) =>
				node is BinaryExpressionSyntax binary
				&& (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)),
			transform: static (ctx, ct) => GetNullableScalarNullComparisonValidationResult(ctx, ct)
		);

		context.RegisterSourceOutput(
			aggregateClasses.Combine(isDisabled),
			(spc, data) =>
			{
				var (result, disabled) = data;
				if (disabled)
					return;

				ReportDiagnostics(spc, result.Diagnostics, _logger);

				if (result.Info is null)
					return;

				var source = EmitHelper.GenerateAggregateSource(result.Info, _logger);
				spc.AddSource(result.Info.HintName, source);
			}
		);

		context.RegisterSourceOutput(
			standaloneEventMethods.Combine(isDisabled),
			(spc, result) =>
			{
				var (validationResult, disabled) = result;
				if (disabled)
					return;

				ReportDiagnostics(spc, validationResult.Diagnostics, _logger);
			}
		);

		context.RegisterSourceOutput(
			manualEventTypes.Combine(isDisabled),
			(spc, result) =>
			{
				var (validationResult, disabled) = result;
				if (disabled)
					return;

				ReportDiagnostics(spc, validationResult.Diagnostics, _logger);
			}
		);

		context.RegisterSourceOutput(
			computedParameterInvocations.Combine(isDisabled),
			(spc, result) =>
			{
				var (validationResult, disabled) = result;
				if (disabled)
					return;

				ReportDiagnostics(spc, validationResult.Diagnostics, _logger);
			}
		);

		context.RegisterSourceOutput(
			nullableScalarNullComparisons.Combine(isDisabled),
			(spc, result) =>
			{
				var (validationResult, disabled) = result;
				if (disabled)
					return;

				ReportDiagnostics(spc, validationResult.Diagnostics, _logger);
			}
		);
	}

	static EventMethodValidationResult GetComputedParameterInvocationValidationResult(
		GeneratorSyntaxContext context,
		CancellationToken ct
	)
	{
		ct.ThrowIfCancellationRequested();
		if (context.Node is not InvocationExpressionSyntax invocation)
			return new([]);

		if (context.SemanticModel.GetOperation(invocation, ct) is not IInvocationOperation invocationOperation)
			return new([]);

		var diagnostics = new List<Diagnostic>();
		foreach (var argument in invocationOperation.Arguments)
		{
			ct.ThrowIfCancellationRequested();
			if (argument.IsImplicit || argument.Parameter is null)
				continue;

			if (!TypeHelpers.HasComputedAttribute(argument.Parameter))
				continue;

			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.ComputedParameterCannotBeSetByCaller,
					argument.Syntax.GetLocation(),
					invocationOperation.TargetMethod.Name,
					argument.Parameter.Name
				)
			);
		}

		return new([.. diagnostics]);
	}

	static EventMethodValidationResult GetNullableScalarNullComparisonValidationResult(
		GeneratorSyntaxContext context,
		CancellationToken ct
	)
	{
		ct.ThrowIfCancellationRequested();
		if (context.Node is not BinaryExpressionSyntax binaryExpression)
			return new([]);

		if (
			!binaryExpression.IsKind(SyntaxKind.EqualsExpression)
			&& !binaryExpression.IsKind(SyntaxKind.NotEqualsExpression)
		)
			return new([]);

		var leftIsNull = binaryExpression.Left.IsKind(SyntaxKind.NullLiteralExpression);
		var rightIsNull = binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression);
		if (leftIsNull == rightIsNull)
			return new([]);

		var enclosingType = context.SemanticModel.GetEnclosingSymbol(binaryExpression.SpanStart, ct)?.ContainingType;
		if (enclosingType is null || !TypeHelpers.HasAttribute(enclosingType, GenerateAggregateAttributeName))
			return new([]);

		var checkedSide = leftIsNull ? binaryExpression.Right : binaryExpression.Left;
		if (
			context.SemanticModel.GetTypeInfo(checkedSide, ct).Type is not INamedTypeSymbol checkedType
			|| !checkedType.IsGenericType
			|| checkedType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T
		)
			return new([]);

		if (checkedType.TypeArguments[0] is not INamedTypeSymbol underlyingType)
			return new([]);

		var isScalarValueObject = underlyingType
			.GetAttributes()
			.Any(attribute => attribute.AttributeClass?.ToDisplayString() == ScalarAttributeMetadataName);
		if (!isScalarValueObject)
			return new([]);

		var recommendedComparison = binaryExpression.IsKind(SyntaxKind.EqualsExpression)
			? $"{checkedSide} is null"
			: $"{checkedSide} is not null";

		return new([
			Diagnostic.Create(
				GeneratorDiagnostics.NullableScalarEqualityNullComparisonShouldUsePatternMatching,
				binaryExpression.GetLocation(),
				binaryExpression.ToString(),
				recommendedComparison
			),
		]);
	}

	static EventMethodValidationResult GetStandaloneEventMethodValidationResult(GeneratorAttributeSyntaxContext ctx)
	{
		return ctx.TargetSymbol is not IMethodSymbol methodSymbol ? new EventMethodValidationResult([])
			: TypeHelpers.HasAttribute(methodSymbol.ContainingType, GenerateAggregateAttributeName)
				? new EventMethodValidationResult([])
			: new EventMethodValidationResult([
				Diagnostic.Create(
					GeneratorDiagnostics.EventMethodRequiresAggregateAttribute,
					ctx.TargetNode.GetLocation(),
					methodSymbol.Name
				),
			]);
	}

	static EventTypeValidationResult GetEventTypeValidationResult(GeneratorSyntaxContext ctx)
	{
		if (ctx.Node is not TypeDeclarationSyntax typeDeclaration)
			return new EventTypeValidationResult([]);

		if (
			ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol
			|| typeSymbol.IsAbstract
		)
			return new EventTypeValidationResult([]);

		if (!TypeHelpers.IsEventType(typeSymbol))
			return new EventTypeValidationResult([]);

		var displayName = GetDisplayEventName(typeSymbol.Name);
		return EventVerbMap.IsPastTenseEventName(displayName)
			? new EventTypeValidationResult([])
			: new EventTypeValidationResult([
				Diagnostic.Create(
					GeneratorDiagnostics.EventNameShouldBePastTense,
					typeDeclaration.Identifier.GetLocation(),
					displayName
				),
			]);
	}

	static bool TryCreateEventMethodInfo(
		INamedTypeSymbol classSymbol,
		IMethodSymbol methodSymbol,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		Compilation compilation,
		INamedTypeSymbol? valueObjectContextType,
		string? aggregateNamespace,
		AttributeStringValue aggregateEventNamespaceOverride,
		AttributeStringValue aggregateEventSuffixOverride,
		AttributeStringValue assemblyEventSuffix,
		List<DiagnosticInfo> diagnostics,
		CancellationToken ct,
		out AggregateEventMethodInfo methodInfo
	)
	{
		var eventSuffix = (
			aggregateEventSuffixOverride.IsPresent ? aggregateEventSuffixOverride.Value
			: assemblyEventSuffix.IsPresent ? assemblyEventSuffix.Value
			: "Event"
		)?.Trim();

		methodInfo = default!;
		var hasErrors = false;
		var methodLocation = methodSymbol.Locations.FirstOrDefault();
		var eventAttribute = methodSymbol
			.GetAttributes()
			.FirstOrDefault(attribute =>
				attribute.AttributeClass?.ToDisplayString() == GenerateEventAttributeName
			);
		var collectionEventAttribute = methodSymbol
			.GetAttributes()
			.FirstOrDefault(attribute =>
				attribute.AttributeClass?.ToDisplayString() == GenerateCollectionEventAttributeName
			);
		var isCollectionEvent = collectionEventAttribute is not null;
		var manualApply = false;

		if (eventAttribute is not null && collectionEventAttribute is not null)
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.UnsupportedEventMethodSignature,
					methodLocation,
					methodSymbol.Name,
					"methods cannot combine [GenerateEvent] and [GenerateAggregateCollectionEvent]"
				)
			);
			return false;
		}

		if (!methodSymbol.IsPartialDefinition)
		{
			diagnostics.Add(
				Diagnostic.Create(GeneratorDiagnostics.EventMethodMustBePartial, methodLocation, methodSymbol.Name)
			);
			return false;
		}

		void ReportUnsupportedSignature(string reason)
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.UnsupportedEventMethodSignature,
					methodLocation,
					methodSymbol.Name,
					reason
				)
			);
			hasErrors = true;
		}

		if (methodSymbol.DeclaredAccessibility == Accessibility.Public && !EventVerbMap.IsVerbPhrase(methodSymbol.Name))
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.AggregateMethodShouldBeVerbPhrase,
					methodLocation,
					methodSymbol.Name
				)
			);
		}

		if (methodSymbol.IsStatic)
			ReportUnsupportedSignature("static methods are not supported");

		if (methodSymbol.TypeParameters.Length > 0)
			ReportUnsupportedSignature("generic methods are not supported");

		if (!TryResolveReturnKind(methodSymbol, classSymbol, out var returnTypeName, out var returnKind))
		{
			ReportUnsupportedSignature("methods must return void, bool, or the containing aggregate type");
		}

		if (methodSymbol.PartialImplementationPart is not null)
			ReportUnsupportedSignature("methods must be partial declarations without a body");

		foreach (var parameter in methodSymbol.Parameters)
		{
			if (parameter.RefKind != RefKind.None)
				ReportUnsupportedSignature("ref, in, and out parameters are not supported");

			if (parameter.IsParams && (!isCollectionEvent || parameter.Type is not IArrayTypeSymbol))
				ReportUnsupportedSignature("params parameters are not supported");
		}

		if (isCollectionEvent)
		{
			if (methodSymbol.Parameters.Length != 1)
				ReportUnsupportedSignature("collection event methods must have exactly one parameter");
		}

		if (hasErrors)
			return false;

		var version = 1;
		var hasExplicitVersion = false;
		var eventName = string.Empty;
		var hasExplicitEventName = false;
		string? eventNamespaceOverride = null;
		AttributeData? activeAttribute = collectionEventAttribute ?? eventAttribute;
		if (activeAttribute is not null)
		{
			foreach (var namedArgument in activeAttribute.NamedArguments)
			{
				if (namedArgument.Key == "Version" && namedArgument.Value.Value is int explicitVersion)
				{
					version = explicitVersion;
					hasExplicitVersion = true;
					continue;
				}

				if (namedArgument.Key == "EventName" && namedArgument.Value.Value is string explicitEventName)
				{
					eventName = explicitEventName.Trim();
					hasExplicitEventName = true;
					continue;
				}

				if (namedArgument.Key == "EventNamespace" && namedArgument.Value.Value is string explicitEventNamespace)
				{
					eventNamespaceOverride = explicitEventNamespace;
					continue;
				}

				if (namedArgument.Key == "Manual" && namedArgument.Value.Value is bool explicitManual)
					manualApply = explicitManual;
			}
		}

		if (version < 1)
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.EventSchemaVersionMustBePositive,
					methodLocation,
					methodSymbol.Name,
					classSymbol.Name,
					version
				)
			);
			hasErrors = true;
		}

		var parameters = new List<EventPropertyInfo>();
		CollectionEventInfo? collectionEvent = null;
		if (isCollectionEvent)
		{
			if (
				!TryCreateCollectionEventInfo(
					methodSymbol,
					collectionEventAttribute!,
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
		}
		else
		{
			foreach (var parameter in methodSymbol.Parameters)
			{
				ct.ThrowIfCancellationRequested();

				var aggregatePropertyName =
					GetAggregatePropertyNameOverride(parameter) ?? EventPropertyInfo.ToPropertyName(parameter.Name);
				var parameterLocation = parameter.Locations.FirstOrDefault() ?? methodLocation;
				var isComputedParameter = HasComputedAttribute(parameter);
				var parameterTypeName = parameter.Type.ToDisplayString(
					SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
						SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
							| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
					)
				);

				if (TryGetMetadataStoreSetting(parameter, out var storeMetadata))
				{
					if (isComputedParameter)
					{
						diagnostics.Add(
							Diagnostic.Create(
								GeneratorDiagnostics.EventParameterMustMapToWritableProperty,
								parameterLocation,
								parameter.Name,
								methodSymbol.Name,
								classSymbol.Name,
								"parameter cannot be marked with both [Metadata] and [Computed]"
							)
						);
						hasErrors = true;
						continue;
					}

					parameters.Add(
						new EventPropertyInfo(
							parameter.Name,
							parameterTypeName,
							parameterTypeName,
							aggregatePropertyName,
							false,
							storeMetadata,
							parameterTypeName,
							parameter.Type.SpecialType == SpecialType.System_String,
							EventParameterConversionKind.None,
							IsComputed: false
						)
					);
					continue;
				}

				if (
					manualApply
					&& (
						!propertySymbolsByName.TryGetValue(aggregatePropertyName, out var manualMappedProperty)
						|| manualMappedProperty.SetMethod is null
						|| manualMappedProperty.SetMethod.IsInitOnly
					)
				)
				{
					parameters.Add(
						new EventPropertyInfo(
							parameter.Name,
							parameterTypeName,
							parameterTypeName,
							aggregatePropertyName,
							false,
							true,
							parameterTypeName,
							parameter.Type.SpecialType == SpecialType.System_String,
							EventParameterConversionKind.None,
							IsComputed: isComputedParameter
						)
					);
					continue;
				}

				if (!propertySymbolsByName.TryGetValue(aggregatePropertyName, out var propertySymbol))
				{
					diagnostics.Add(
						Diagnostic.Create(
							GeneratorDiagnostics.EventParameterMustMapToWritableProperty,
							parameterLocation,
							parameter.Name,
							methodSymbol.Name,
							classSymbol.Name,
							$"property '{aggregatePropertyName}' does not exist"
						)
					);
					hasErrors = true;
					continue;
				}

				if (propertySymbol.SetMethod is null)
				{
					diagnostics.Add(
						Diagnostic.Create(
							GeneratorDiagnostics.EventParameterMustMapToWritableProperty,
							parameterLocation,
							parameter.Name,
							methodSymbol.Name,
							classSymbol.Name,
							$"property '{aggregatePropertyName}' does not have a setter"
						)
					);
					hasErrors = true;
					continue;
				}

				if (propertySymbol.SetMethod.IsInitOnly)
				{
					diagnostics.Add(
						Diagnostic.Create(
							GeneratorDiagnostics.EventParameterMustMapToWritableProperty,
							parameterLocation,
							parameter.Name,
							methodSymbol.Name,
							classSymbol.Name,
							$"property '{aggregatePropertyName}' is init-only"
						)
					);
					hasErrors = true;
					continue;
				}

				var propertyTypeName = propertySymbol.Type.ToDisplayString(
					SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
						SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
							| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
					)
				);

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
						Diagnostic.Create(
							GeneratorDiagnostics.EventParameterMustMapToWritableProperty,
							parameterLocation,
							parameter.Name,
							methodSymbol.Name,
							classSymbol.Name,
							$"parameter type '{parameterTypeName}' cannot be mapped to property '{aggregatePropertyName}' of type '{propertyTypeName}' via implicit conversion or value-object Create(...)"
						)
					);
					hasErrors = true;
					continue;
				}

				// Warn when a non-nullable parameter maps to a nullable property via nullability widening.
				// The generator works around this automatically (see ResolveParameterConversionKind), but
				// the right long-term fix is to align the parameter's nullability with the property.
				if (
					conversionKind == EventParameterConversionKind.Implicit
					&& SymbolEqualityComparer.Default.Equals(parameter.Type, propertySymbol.Type)
					&& propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated
					&& parameter.Type.NullableAnnotation != NullableAnnotation.Annotated
				)
				{
					diagnostics.Add(
						Diagnostic.Create(
							GeneratorDiagnostics.EventParameterNullabilityMismatch,
							parameterLocation,
							parameter.Name,
							methodSymbol.Name,
							aggregatePropertyName,
							propertyTypeName
						)
					);
				}

				parameters.Add(
					new EventPropertyInfo(
						parameter.Name,
						parameterTypeName,
						propertyTypeName,
						propertySymbol.Name,
						true,
						true,
						propertyTypeName,
						parameter.Type.SpecialType == SpecialType.System_String
							&& propertySymbol.Type.SpecialType == SpecialType.System_String,
						conversionKind.Value,
						IsComputed: isComputedParameter
					)
				);
			}
		}

		if (hasErrors)
			return false;

		if (hasExplicitEventName)
		{
			if (!EventVerbMap.IsPastTenseEventName(eventName))
			{
				diagnostics.Add(
					Diagnostic.Create(
						GeneratorDiagnostics.EventNameOverrideShouldBePastTense,
						methodLocation,
						eventName,
						methodSymbol.Name
					)
				);
			}
		}
		else if (!EventVerbMap.TryCreateGeneratedEventName(methodSymbol.Name, classSymbol.Name, out eventName))
		{
			var suggestedMethodName = EventVerbMap.TrySuggestVerbPhrase(methodSymbol.Name, out var suggestedVerbPhrase)
				? suggestedVerbPhrase
				: $"Create{TrimAggregateSuffix(classSymbol.Name)}";

			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.UnableToInferEventName,
					methodLocation,
					methodSymbol.Name,
					suggestedMethodName
				)
			);
			hasErrors = true;
		}
		else
		{
			if (
				isCollectionEvent
				&& collectionEvent is not null
				&& collectionEvent.ParameterShape == CollectionParameterShape.Array
			)
				eventName += "Array";

			eventName += eventSuffix;
		}

		if (hasErrors)
			return false;

		var eventNamespace = string.IsNullOrWhiteSpace(eventNamespaceOverride)
			? string.IsNullOrWhiteSpace(aggregateEventNamespaceOverride.Value)
				? CreateDefaultEventNamespace(aggregateNamespace, classSymbol.Name)
				: aggregateEventNamespaceOverride!.Value!.Trim()
			: eventNamespaceOverride!.Trim();

		methodInfo = new AggregateEventMethodInfo(
			methodSymbol.Name,
			eventName,
			eventNamespace,
			parameters,
			returnTypeName,
			returnKind,
			methodSymbol.DeclaredAccessibility,
			version,
			hasExplicitVersion,
			manualApply,
			collectionEvent
		);
		return true;
	}

	static bool TryCreateCollectionEventInfo(
		IMethodSymbol methodSymbol,
		AttributeData collectionEventAttribute,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		List<Diagnostic> diagnostics,
		out EventPropertyInfo parameterInfo,
		out CollectionEventInfo? collectionEvent
	)
	{
		parameterInfo = default!;
		collectionEvent = null;

		var methodLocation = methodSymbol.Locations.FirstOrDefault();
		if (methodSymbol.Parameters.Length != 1)
			return false;

		if (
			collectionEventAttribute.ConstructorArguments.Length != 1
			|| collectionEventAttribute.ConstructorArguments[0].Value is not string rawPropertyName
			|| string.IsNullOrWhiteSpace(rawPropertyName)
		)
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.UnsupportedEventMethodSignature,
					methodLocation,
					methodSymbol.Name,
					"collection property name must be provided via [GenerateAggregateCollectionEvent(nameof(CollectionProperty))]"
				)
			);
			return false;
		}

		var collectionPropertyName = rawPropertyName.Trim();
		if (!propertySymbolsByName.TryGetValue(collectionPropertyName, out var collectionProperty))
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.EventParameterMustMapToWritableProperty,
					methodLocation,
					methodSymbol.Parameters[0].Name,
					methodSymbol.Name,
					methodSymbol.ContainingType.Name,
					$"collection property '{collectionPropertyName}' does not exist"
				)
			);
			return false;
		}

		if (!TryGetCollectionDetails(collectionProperty.Type, out var elementType, out var isSet))
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.EventParameterMustMapToWritableProperty,
					methodLocation,
					methodSymbol.Parameters[0].Name,
					methodSymbol.Name,
					methodSymbol.ContainingType.Name,
					$"collection property '{collectionPropertyName}' must use Purview.EventSourcing.EventStoreList<T> or Purview.EventSourcing.EventStoreSet<T>"
				)
			);
			return false;
		}

		var parameter = methodSymbol.Parameters[0];
		var parameterType = parameter.Type;
		var parameterTypeName = parameterType.ToDisplayString(
			SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
				SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
					| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
			)
		);
		var elementTypeName = elementType.ToDisplayString(
			SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
				SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
					| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
			)
		);

		CollectionParameterShape parameterShape;
		string eventPropertyTypeName;
		if (SymbolEqualityComparer.Default.Equals(parameterType, elementType))
		{
			parameterShape = CollectionParameterShape.Single;
			eventPropertyTypeName = elementTypeName;
		}
		else if (
			parameterType is IArrayTypeSymbol arrayType
			&& SymbolEqualityComparer.Default.Equals(arrayType.ElementType, elementType)
		)
		{
			parameterShape = CollectionParameterShape.Array;
			eventPropertyTypeName = $"{elementTypeName}[]";
		}
		else if (TryGetIEnumerableElementType(parameterType, out var enumerableElementType))
		{
			if (!SymbolEqualityComparer.Default.Equals(enumerableElementType, elementType))
			{
				diagnostics.Add(
					Diagnostic.Create(
						GeneratorDiagnostics.UnsupportedEventMethodSignature,
						parameter.Locations.FirstOrDefault() ?? methodLocation,
						methodSymbol.Name,
						$"collection item type '{parameterTypeName}' does not match '{elementTypeName}'"
					)
				);
				return false;
			}

			parameterShape = CollectionParameterShape.Enumerable;
			eventPropertyTypeName = $"{elementTypeName}[]";
		}
		else
		{
			diagnostics.Add(
				Diagnostic.Create(
					GeneratorDiagnostics.UnsupportedEventMethodSignature,
					parameter.Locations.FirstOrDefault() ?? methodLocation,
					methodSymbol.Name,
					$"collection methods only support '{elementTypeName}', '{elementTypeName}[]', or IEnumerable<{elementTypeName}> parameters"
				)
			);
			return false;
		}

		parameterInfo = new EventPropertyInfo(
			parameter.Name,
			parameterTypeName,
			eventPropertyTypeName,
			collectionPropertyName,
			HasAggregateProperty: false,
			IncludeInEvent: true,
			EqualityComparerTypeName: eventPropertyTypeName,
			UseStringOrdinalComparison: false,
			ParameterConversionKind: EventParameterConversionKind.None,
			IsComputed: false,
			IsParams: parameter.IsParams
		);

		if (
			!TryResolveCollectionMutationOperation(
				methodSymbol,
				collectionEventAttribute,
				diagnostics,
				methodLocation,
				out var mutationOperation
			)
		)
		{
			return false;
		}

		collectionEvent = new CollectionEventInfo(
			collectionPropertyName,
			elementTypeName,
			collectionProperty.Type.ToDisplayString(
				SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
					SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
						| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
				)
			),
			isSet,
			mutationOperation,
			parameterShape,
			methodSymbol.Name
		);

		return true;
	}

	static bool TryResolveCollectionMutationOperation(
		IMethodSymbol methodSymbol,
		AttributeData collectionEventAttribute,
		List<Diagnostic> diagnostics,
		Location? methodLocation,
		out CollectionMutationOperation operation
	)
	{
		operation = CollectionMutationOperation.Add;

		foreach (var namedArgument in collectionEventAttribute.NamedArguments)
		{
			if (namedArgument.Key != "Operation")
				continue;

			if (namedArgument.Value.Value is int operationValue)
			{
				if (operationValue == 1)
				{
					operation = CollectionMutationOperation.Add;
					return true;
				}

				if (operationValue == 2)
				{
					operation = CollectionMutationOperation.Remove;
					return true;
				}
			}

			return TryInferCollectionMutationOperation(methodSymbol, diagnostics, methodLocation, out operation);
		}

		return TryInferCollectionMutationOperation(methodSymbol, diagnostics, methodLocation, out operation);
	}

	static bool TryInferCollectionMutationOperation(
		IMethodSymbol methodSymbol,
		List<Diagnostic> diagnostics,
		Location? methodLocation,
		out CollectionMutationOperation operation
	)
	{
		operation = CollectionMutationOperation.Add;

		if (methodSymbol.Name.StartsWith("Add", StringComparison.Ordinal))
		{
			operation = CollectionMutationOperation.Add;
			return true;
		}

		if (
			methodSymbol.Name.StartsWith("Remove", StringComparison.Ordinal)
			|| methodSymbol.Name.StartsWith("Delete", StringComparison.Ordinal)
		)
		{
			operation = CollectionMutationOperation.Remove;
			return true;
		}

		diagnostics.Add(
			Diagnostic.Create(
				GeneratorDiagnostics.UnsupportedEventMethodSignature,
				methodLocation,
				methodSymbol.Name,
				"collection event methods must begin with 'Add', 'Remove', or 'Delete', or explicitly set Operation = CollectionEventOperation.Add/Remove"
			)
		);

		return false;
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
		return conversion.Exists && conversion.IsImplicit ? EventParameterConversionKind.Implicit : null;
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

		var hasScalarAttribute = propertyType
			.GetAttributes()
			.Any(attribute => attribute.AttributeClass?.ToDisplayString() == ScalarAttributeMetadataName);
		var createMethods = propertyType.GetMembers("Create").OfType<IMethodSymbol>().ToArray();

		var hasContextualCreate = createMethods.Any(method =>
			IsContextualCreateMethod(method, propertyType, aggregateType, parameterType, contextTypeDefinition)
		);

		if (hasContextualCreate)
		{
			conversionKind = EventParameterConversionKind.ContextualCreate;
			return true;
		}

		var hasSimpleCreate = createMethods.Any(method => IsSimpleCreateMethod(method, propertyType, parameterType));

		if (hasSimpleCreate || hasScalarAttribute)
		{
			conversionKind = EventParameterConversionKind.Create;
			return true;
		}

		return false;
	}

	static bool IsSimpleCreateMethod(IMethodSymbol method, ITypeSymbol returnType, ITypeSymbol parameterType)
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
		if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Public || method.Name != "Create")
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
			&& SymbolEqualityComparer.Default.Equals(contextType.OriginalDefinition, contextTypeDefinition)
			&& contextType.TypeArguments.Length == 1
			&& SymbolEqualityComparer.Default.Equals(contextType.TypeArguments[0], aggregateType);
	}

	static AttributeStringValue GetAttributeStringNamedArgument(
		ImmutableArray<AttributeData> attributes,
		string attributeMetadataName,
		string argumentName
	)
	{
		foreach (var attribute in attributes)
		{
			var attributeClass = attribute.AttributeClass;
			if (attributeClass is null || attributeClass.ToDisplayString() != attributeMetadataName)
				continue;

			foreach (var namedArgument in attribute.NamedArguments)
			{
				if (namedArgument.Key == argumentName && namedArgument.Value.Value is string value)
					return new(value, true);
			}
		}

		return new AttributeStringValue(null, false);
	}


	static string GetDisplayEventName(string eventName) =>
		eventName.EndsWith("Event", StringComparison.Ordinal)
			? eventName.Substring(0, eventName.Length - "Event".Length)
			: eventName;

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
}
