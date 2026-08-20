using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static class AggregateInfoBuilder
{
	public static GeneratorResult<AggregateInfo> Build(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var classSymbol = (INamedTypeSymbol)context.TargetSymbol;
		var syntax = (ClassDeclarationSyntax)context.TargetNode;

		var compilation = context.SemanticModel.Compilation;
		List<DiagnosticInfo> diagnostics = [];

		var canGenerate = ValidateAggregateClass(
			classSymbol,
			syntax,
			diagnostics,
			out var shouldDeclareAggregateBase
		);

		TypeValueObject aggregateType = new(classSymbol);
		var aggregateAttribute = AggregateAttributeData.FromAttributeData(classSymbol);
		var assemblyDefaults = AggregateDefaultsAttributeData.FromAttributeData(
			compilation.Assembly
		);

		var aggregateNamespace = aggregateType.Namespace;
		var eventNamespaceOverride = aggregateAttribute.Exists
			? aggregateAttribute.EventNamespace
			: null;
		var aggregateEventSuffixOverride = aggregateAttribute.Exists
			? aggregateAttribute.EventSuffix
			: null;
		var assemblyEventSuffix = assemblyDefaults.Exists ? assemblyDefaults.EventSuffix : null;
		var valueObjectContextType = compilation.GetTypeByMetadataName(
			"Purview.EventSourcing.ValueObjects.ValueObjectContext`1"
		);

		List<AggregateStatePropertyInfo> properties = [];
		Dictionary<string, IPropertySymbol> propertySymbolsByName = [with(StringComparer.Ordinal)];
		List<IMethodSymbol> attributedMethods = [];

		ScanProperties(
			classSymbol,
			diagnostics,
			properties,
			propertySymbolsByName,
			attributedMethods,
			cancellationToken
		);

		var methods = new List<AggregateEventMethodInfo>();
		var invalidMethods = new List<InvalidAggregateEventMethodInfo>();
		var methodsByEventType = new Dictionary<TypeReferenceOptions, IMethodSymbol>();
		var methodsBySchemaVersion = new Dictionary<int, (IMethodSymbol Symbol, bool IsExplicit)>();

		BuildMethods(
			classSymbol,
			compilation,
			valueObjectContextType,
			aggregateNamespace,
			eventNamespaceOverride,
			aggregateEventSuffixOverride,
			assemblyEventSuffix,
			propertySymbolsByName,
			attributedMethods,
			methods,
			invalidMethods,
			methodsByEventType,
			methodsBySchemaVersion,
			diagnostics,
			cancellationToken
		);

		ValidateSchemaVersionContiguity(syntax, classSymbol, methods, diagnostics);

		return canGenerate
			? GeneratorResult<AggregateInfo>.Ok(
				new(
					aggregateType,
					classSymbol.DeclaredAccessibility,
					shouldDeclareAggregateBase,
					properties,
					methods,
					invalidMethods,
					AggregateEventMethodBuilder.CreateHintName(classSymbol)
				),
				[.. diagnostics]
			)
			: GeneratorResult<AggregateInfo>.Fail([.. diagnostics]);
	}

	static bool ValidateAggregateClass(
		INamedTypeSymbol classSymbol,
		ClassDeclarationSyntax syntax,
		List<DiagnosticInfo> diagnostics,
		out bool shouldDeclareAggregateBase
	)
	{
		shouldDeclareAggregateBase = false;
		var canGenerate = true;

		var isPartial = TypeHelpers.IsPartial(syntax);
		if (!isPartial)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.AggregateMustBePartial,
					syntax.Identifier.GetLocation(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		if (classSymbol.ContainingType is not null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.NestedAggregatesAreNotSupported,
					syntax.Identifier.GetLocation(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		if (classSymbol.TypeParameters.Length > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.GenericAggregatesAreNotSupported,
					syntax.Identifier.GetLocation(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		if (!TypeHelpers.InheritsFrom(classSymbol, TypeLibrary.Aggregates.AggregateBase))
		{
			if (
				classSymbol.BaseType is null
				|| classSymbol.BaseType.SpecialType == SpecialType.System_Object
			)
			{
				shouldDeclareAggregateBase = true;
			}
			else
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.AggregateMustInheritAggregateBase,
						syntax.Identifier.GetLocation(),
						classSymbol.Name
					)
				);
				canGenerate = false;
			}
		}

		if (
			AggregateEventMethodBuilder.HasRegisterEventsMethod(
				classSymbol,
				out var registerEventsMethod
			)
		)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.ManualRegisterEventsIsNotSupported,
					registerEventsMethod!.Locations.FirstOrDefault(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		return canGenerate;
	}

	static void ScanProperties(
		INamedTypeSymbol classSymbol,
		List<DiagnosticInfo> diagnostics,
		List<AggregateStatePropertyInfo> properties,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		List<IMethodSymbol> attributedMethods,
		CancellationToken cancellationToken
	)
	{
		foreach (var member in classSymbol.GetMembers())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (member is IPropertySymbol propertySymbol)
			{
				if (
					propertySymbol.IsStatic
					|| propertySymbol.IsIndexer
					|| propertySymbol.IsImplicitlyDeclared
				)
					continue;

				if (
					AggregateEventMethodBuilder.TryGetComplexScalarValueType(
						propertySymbol.Type,
						out var scalarValueTypeDisplayName
					)
				)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.ScalarComplexValueMayNotTranslateInSqlSnapshots,
							propertySymbol.Locations.FirstOrDefault(),
							propertySymbol.Name,
							classSymbol.Name,
							scalarValueTypeDisplayName
						)
					);
				}

				propertySymbolsByName[propertySymbol.Name] = propertySymbol;

				if (propertySymbol.SetMethod is null)
					continue;

				if (propertySymbol.SetMethod.DeclaredAccessibility is not Accessibility.Private)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.AggregatePropertySetterShouldBePrivate,
							propertySymbol.SetMethod.Locations.FirstOrDefault()
								?? propertySymbol.Locations.FirstOrDefault(),
							propertySymbol.Name,
							classSymbol.Name,
							propertySymbol.SetMethod.DeclaredAccessibility.ToString()
						)
					);
				}

				if (
					AggregateEventMethodBuilder.IsCollectionLikeType(propertySymbol.Type)
					&& !AggregateEventMethodBuilder.IsEventStoreCollectionType(propertySymbol.Type)
				)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.AggregatePropertyCollectionTypeMustUseEventStoreCollections,
							propertySymbol.Locations.FirstOrDefault(),
							propertySymbol.Name,
							classSymbol.Name,
							propertySymbol.Type.ToDisplayString()
						)
					);
				}

				properties.Add(new(propertySymbol.Name, new(propertySymbol.Type)));
				continue;
			}

			if (
				member is IMethodSymbol methodSymbol
				&& (
					AggregateEventMethodBuilder.HasAttribute(
						methodSymbol,
						TypeLibrary.Attributes.EventAttribute
					)
					|| AggregateEventMethodBuilder.HasAttribute(
						methodSymbol,
						TypeLibrary.Attributes.CollectionEventAttribute
					)
				)
			)
			{
				attributedMethods.Add(methodSymbol);
			}
		}
	}

	static void BuildMethods(
		INamedTypeSymbol classSymbol,
		Compilation compilation,
		INamedTypeSymbol? valueObjectContextType,
		string? aggregateNamespace,
		string? eventNamespaceOverride,
		string? aggregateEventSuffixOverride,
		string? assemblyEventSuffix,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		List<IMethodSymbol> attributedMethods,
		List<AggregateEventMethodInfo> methods,
		List<InvalidAggregateEventMethodInfo> invalidMethods,
		Dictionary<TypeReferenceOptions, IMethodSymbol> methodsByEventType,
		Dictionary<int, (IMethodSymbol Symbol, bool IsExplicit)> methodsBySchemaVersion,
		List<DiagnosticInfo> diagnostics,
		CancellationToken cancellationToken
	)
	{
		foreach (var methodSymbol in attributedMethods)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var diagnosticsStart = diagnostics.Count;

			if (
				!AggregateEventMethodBuilder.TryBuild(
					classSymbol,
					methodSymbol,
					propertySymbolsByName,
					compilation,
					valueObjectContextType,
					aggregateNamespace,
					eventNamespaceOverride,
					aggregateEventSuffixOverride,
					assemblyEventSuffix,
					diagnostics,
					cancellationToken,
					out var methodInfo
				)
			)
			{
				var diagnosticIds = diagnostics
					.Skip(diagnosticsStart)
					.Select(static diagnostic => diagnostic.Descriptor.Id)
					.Distinct(StringComparer.Ordinal)
					.OrderBy(static id => id, StringComparer.Ordinal)
					.ToArray();

				if (
					AggregateEventMethodBuilder.TryCreateInvalidMethodStub(
						methodSymbol,
						diagnosticIds,
						out var invalidMethod,
						cancellationToken
					)
				)
					invalidMethods.Add(invalidMethod);

				continue;
			}

			if (methodsByEventType.TryGetValue(methodInfo.EventType, out var conflictingMethod))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.DuplicateGeneratedEventName,
						methodSymbol,
						methodSymbol.Name,
						classSymbol.Name,
						methodInfo.EventType.TypeName
					)
				);
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.DuplicateGeneratedEventName,
						conflictingMethod,
						conflictingMethod.Name,
						classSymbol.Name,
						methodInfo.EventType.TypeName
					)
				);

				if (
					AggregateEventMethodBuilder.TryCreateInvalidMethodStub(
						methodSymbol,
						[DiagnosticLibrary.DuplicateGeneratedEventName.Id],
						out var invalidMethod,
						cancellationToken
					)
				)
					invalidMethods.Add(invalidMethod);

				continue;
			}

			if (
				methodsBySchemaVersion.TryGetValue(
					methodInfo.Version,
					out var existingSchemaVersionMethod
				)
			)
			{
				if (methodInfo.IsSchemaVersionExplicit && existingSchemaVersionMethod.IsExplicit)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate,
							methodSymbol,
							methodSymbol.Name,
							classSymbol.Name,
							$"{methodInfo.Version}",
							existingSchemaVersionMethod.Symbol.Name
						)
					);
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate,
							existingSchemaVersionMethod.Symbol,
							existingSchemaVersionMethod.Symbol.Name,
							classSymbol.Name,
							methodInfo.Version,
							methodSymbol.Name
						)
					);

					if (
						AggregateEventMethodBuilder.TryCreateInvalidMethodStub(
							methodSymbol,
							[DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate.Id],
							out var invalidMethod,
							cancellationToken
						)
					)
						invalidMethods.Add(invalidMethod);

					continue;
				}

				if (methodInfo.IsSchemaVersionExplicit && !existingSchemaVersionMethod.IsExplicit)
					methodsBySchemaVersion[methodInfo.Version] = (methodSymbol, true);
			}
			else
			{
				methodsBySchemaVersion[methodInfo.Version] = (
					methodSymbol,
					methodInfo.IsSchemaVersionExplicit
				);
			}

			methodsByEventType[methodInfo.EventType] = methodSymbol;
			methods.Add(methodInfo);
		}
	}

	static void ValidateSchemaVersionContiguity(
		ClassDeclarationSyntax syntax,
		INamedTypeSymbol classSymbol,
		List<AggregateEventMethodInfo> methods,
		List<DiagnosticInfo> diagnostics
	)
	{
		var explicitSchemaVersions = methods
			.Where(static method => method.IsSchemaVersionExplicit)
			.Select(static method => method.Version)
			.Distinct()
			.OrderBy(static version => version)
			.ToArray();

		if (explicitSchemaVersions.Length < 2)
			return;

		var missingSchemaVersions = new List<int>();
		for (var index = 1; index < explicitSchemaVersions.Length; index++)
		{
			var previousVersion = explicitSchemaVersions[index - 1];
			var currentVersion = explicitSchemaVersions[index];

			for (
				var missingVersion = previousVersion + 1;
				missingVersion < currentVersion;
				missingVersion++
			)
				missingSchemaVersions.Add(missingVersion);
		}

		if (missingSchemaVersions.Count > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.EventSchemaVersionsShouldBeContiguous,
					syntax.Identifier.GetLocation(),
					classSymbol.Name,
					string.Join(", ", explicitSchemaVersions),
					string.Join(", ", missingSchemaVersions)
				)
			);
		}
	}
}
