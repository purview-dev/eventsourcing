using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static class ComplexValueObjectModelBuilder
{
	public static ComplexValueObjectModel? Build(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken,
		out ImmutableArray<DiagnosticInfo> diagnostics
	)
	{
		cancellationToken.ThrowIfCancellationRequested();
		diagnostics = [];

		if (context.TargetSymbol is not INamedTypeSymbol typeSymbol || context.TargetNode is not TypeDeclarationSyntax)
			return null;

		var location = context.TargetNode.GetLocation();
		var diagnosticsList = new List<DiagnosticInfo>();
		diagnosticsList.AddRange(
			ValueObjectSymbolInspector.ValidateValueObjectType(typeSymbol, "ValueObject", location)
		);

		var attributes = typeSymbol.GetAttributes();
		if (ValueObjectSymbolInspector.HasAttribute(attributes, ValueObjectSymbolInspector.ScalarAttributeName))
		{
			diagnosticsList.Add(
				DiagnosticInfo.Create(DiagnosticLibrary.ConflictingValueObjectAttributes, location, typeSymbol.Name)
			);
			diagnostics = [.. diagnosticsList];
			return null;
		}

		var assemblyDefaults = ValueObjectDefaultsAttributeData.FromAttributeData(
			context.SemanticModel.Compilation.Assembly.GetAttributes()
		);
		var valueObjectOptions = ValueObjectAttributeData.FromAttributeData(attributes);
		var effectiveGenerateConstructor =
			IsPropertyExplicitlySet(
				attributes,
				ValueObjectSymbolInspector.ValueObjectAttributeName,
				"GenerateConstructor"
			)
				? valueObjectOptions.GenerateConstructor
			: assemblyDefaults.Exists ? assemblyDefaults.GenerateConstructor
			: true;
		valueObjectOptions = valueObjectOptions with { GenerateConstructor = effectiveGenerateConstructor };

		var typeModel = ValueObjectSymbolInspector.BuildTypeModel(typeSymbol);
		if (typeModel is null)
		{
			diagnostics = [.. diagnosticsList];
			return null;
		}

		var properties = typeSymbol
			.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(property =>
				!property.IsStatic
				&& !property.IsIndexer
				&& property.GetMethod is not null
				&& ValueObjectSymbolInspector.IsValueObjectPropertyCandidate(typeSymbol, property)
				&& SymbolEqualityComparer.Default.Equals(property.ContainingType, typeSymbol)
			)
			.OrderBy(property => property.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
			.ToArray();

		var ctorExists = typeSymbol
			.Constructors.Where(static ctor => !ctor.IsStatic)
			.Any(ctor => ValueObjectSymbolInspector.ConstructorMatches(ctor, properties));

		var propertyTypeNames = properties
			.Select(property => ValueObjectSymbolInspector.ToTypeName(property.Type))
			.ToArray();
		var propertyNames = properties.Select(property => property.Name).ToArray();

		var hydrateExists = ValueObjectSymbolInspector.HasStaticFactory(
			typeSymbol,
			"Hydrate",
			[.. properties.Select(property => property.Type)]
		);
		var compareToSelfExists = ValueObjectSymbolInspector.HasInstanceMethod(typeSymbol, "CompareTo", [typeSymbol]);
		var compareToObjectExists = ValueObjectSymbolInspector.HasCompareToObject(typeSymbol);
		var isReferenceType = typeSymbol.TypeKind == TypeKind.Class;
		var compareToSelfParameterTypeName = isReferenceType
			? $"{typeModel.Value.FullyQualifiedName}?"
			: typeModel.Value.FullyQualifiedName;
		var equalsSelfExists =
			typeSymbol.IsRecord || ValueObjectSymbolInspector.HasInstanceMethod(typeSymbol, "Equals", [typeSymbol]);
		var equalsObjectExists = ValueObjectSymbolInspector.HasEqualsObject(typeSymbol);
		var getHashCodeExists = ValueObjectSymbolInspector.HasParameterlessMethod(typeSymbol, "GetHashCode");
		var equalityOperatorExists =
			typeSymbol.IsRecord
			|| ValueObjectSymbolInspector.HasBinaryOperator(typeSymbol, "op_Equality", [typeSymbol, typeSymbol]);
		var inequalityOperatorExists =
			typeSymbol.IsRecord
			|| ValueObjectSymbolInspector.HasBinaryOperator(typeSymbol, "op_Inequality", [typeSymbol, typeSymbol]);
		var hasJsonConverterAttribute = ValueObjectSymbolInspector.HasAttribute(
			typeSymbol,
			ValueObjectSymbolInspector.JsonConverterAttributeName
		);
		var createExists = ValueObjectSymbolInspector.HasStaticFactory(
			typeSymbol,
			"Create",
			[.. properties.Select(property => property.Type)]
		);
		var declareOnNormalize = ValueObjectSymbolInspector.ShouldEmitComplexHookDeclaration(
			typeSymbol,
			"OnNormalize",
			propertyNames.Length,
			includeRef: true
		);

		var parameterlessCtorExists = typeSymbol
			.Constructors.Where(static ctor => !ctor.IsStatic)
			.Any(ctor => ctor.Parameters.Length == 0 && !ctor.IsImplicitlyDeclared);

		var hydrateFactoryName =
			valueObjectOptions.DeserializationMode == ValueObjectSymbolInspector.StrictModeName ? "Create" : "Hydrate";

		var hintName = ValueObjectSymbolInspector.BuildHintName(typeSymbol, "ComplexValueObject");

		diagnostics = [.. diagnosticsList];
		return new ComplexValueObjectModel(
			typeSymbol,
			typeModel.Value,
			properties,
			valueObjectOptions,
			ctorExists,
			hintName,
			typeModel.Value.FullyQualifiedName,
			propertyTypeNames,
			propertyNames,
			hydrateExists,
			compareToSelfExists,
			compareToObjectExists,
			isReferenceType,
			compareToSelfParameterTypeName,
			equalsSelfExists,
			equalsObjectExists,
			getHashCodeExists,
			equalityOperatorExists,
			inequalityOperatorExists,
			hasJsonConverterAttribute,
			createExists,
			declareOnNormalize,
			parameterlessCtorExists,
			hydrateFactoryName
		);
	}

	static bool IsPropertyExplicitlySet(
		ImmutableArray<AttributeData> attributes,
		string attributeName,
		string propertyName
	)
	{
		var attribute = attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeName);
		return attribute?.NamedArguments.Any(kvp => kvp.Key == propertyName) ?? false;
	}
}
