using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static class ScalarValueObjectModelBuilder
{
	public static ScalarValueObjectModel? Build(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken,
		out ImmutableArray<DiagnosticInfo> diagnostics
	)
	{
		cancellationToken.ThrowIfCancellationRequested();
		diagnostics = [];

		if (
			context.TargetSymbol is not INamedTypeSymbol typeSymbol
			|| context.TargetNode is not TypeDeclarationSyntax
		)
			return null;

		var location = context.TargetNode.GetLocation();
		var diagnosticsList = new List<DiagnosticInfo>();
		diagnosticsList.AddRange(
			ValueObjectSymbolInspector.ValidateValueObjectType(typeSymbol, "Scalar", location)
		);

		var attributes = typeSymbol.GetAttributes();
		if (
			ValueObjectSymbolInspector.HasAttribute(
				attributes,
				ValueObjectSymbolInspector.ValueObjectAttributeName
			)
		)
		{
			diagnosticsList.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.ConflictingValueObjectAttributes,
					location,
					typeSymbol.Name
				)
			);
			diagnostics = [.. diagnosticsList];
			return null;
		}

		var scalarOptions = ScalarAttributeData.FromAttributeData(attributes);
		var scalarProperty = typeSymbol
			.GetMembers(scalarOptions.PropertyName)
			.OfType<IPropertySymbol>()
			.FirstOrDefault(property => !property.IsStatic && property.GetMethod is not null);

		if (scalarProperty is null)
		{
			diagnosticsList.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.ScalarPropertyMissing,
					location,
					typeSymbol.Name,
					scalarOptions.PropertyName
				)
			);
			diagnostics = [.. diagnosticsList];
			return null;
		}

		var ctorExists = typeSymbol
			.Constructors.Where(static ctor => !ctor.IsStatic)
			.Any(ctor =>
				ctor.Parameters.Length == 1
				&& SymbolEqualityComparer.Default.Equals(
					ctor.Parameters[0].Type,
					scalarProperty.Type
				)
			);

		var typeModel = ValueObjectSymbolInspector.BuildTypeModel(typeSymbol);
		if (typeModel is null)
		{
			diagnostics = [.. diagnosticsList];
			return null;
		}

		if (typeSymbol.TypeKind == TypeKind.Struct && !typeSymbol.IsRecord)
		{
			diagnosticsList.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.ScalarShouldBeRecordStruct,
					location,
					typeSymbol.Name
				)
			);
		}

		var typeName = typeModel.Value.FullyQualifiedName;
		var scalarTypeName = ValueObjectSymbolInspector.ToTypeName(scalarProperty.Type);
		var compareParameterTypeName = scalarProperty.Type.IsReferenceType
			? $"{scalarTypeName}?"
			: scalarTypeName;
		var compareToSelfParameterTypeName =
			typeSymbol.TypeKind == TypeKind.Class ? $"{typeName}?" : typeName;
		var scalarCanBeNull =
			scalarProperty.Type.IsReferenceType
			|| scalarProperty.Type.NullableAnnotation == NullableAnnotation.Annotated;
		var isReferenceType = typeSymbol.TypeKind == TypeKind.Class;
		var scalarPropertyName = scalarProperty.Name;
		var createExists = ValueObjectSymbolInspector.HasStaticFactory(
			typeSymbol,
			"Create",
			[scalarProperty.Type]
		);
		var hydrateExists = ValueObjectSymbolInspector.HasStaticFactory(
			typeSymbol,
			"Hydrate",
			[scalarProperty.Type]
		);
		var tryCreateExists = ValueObjectSymbolInspector.HasTryCreate(
			typeSymbol,
			scalarProperty.Type
		);
		var compareToSelfExists = ValueObjectSymbolInspector.HasInstanceMethod(
			typeSymbol,
			"CompareTo",
			[typeSymbol]
		);
		var compareToPrimitiveExists = ValueObjectSymbolInspector.HasInstanceMethod(
			typeSymbol,
			"CompareTo",
			[scalarProperty.Type]
		);
		var compareToObjectExists = ValueObjectSymbolInspector.HasCompareToObject(typeSymbol);
		var equalsSelfExists =
			typeSymbol.IsRecord
			|| ValueObjectSymbolInspector.HasInstanceMethod(typeSymbol, "Equals", [typeSymbol]);
		var equalsPrimitiveExists = ValueObjectSymbolInspector.HasInstanceMethod(
			typeSymbol,
			"Equals",
			[scalarProperty.Type]
		);
		var equalsObjectExists = ValueObjectSymbolInspector.HasEqualsObject(typeSymbol);
		var getHashCodeExists = ValueObjectSymbolInspector.HasParameterlessMethod(
			typeSymbol,
			"GetHashCode"
		);
		var sameTypeEqualityOperatorExists =
			typeSymbol.IsRecord
			|| ValueObjectSymbolInspector.HasBinaryOperator(
				typeSymbol,
				"op_Equality",
				[typeSymbol, typeSymbol]
			);
		var sameTypeInequalityOperatorExists =
			typeSymbol.IsRecord
			|| ValueObjectSymbolInspector.HasBinaryOperator(
				typeSymbol,
				"op_Inequality",
				[typeSymbol, typeSymbol]
			);
		var primitiveEqualityOperatorExists = ValueObjectSymbolInspector.HasBinaryOperator(
			typeSymbol,
			"op_Equality",
			[typeSymbol, scalarProperty.Type]
		);
		var primitiveInequalityOperatorExists = ValueObjectSymbolInspector.HasBinaryOperator(
			typeSymbol,
			"op_Inequality",
			[typeSymbol, scalarProperty.Type]
		);
		var reversePrimitiveEqualityOperatorExists = ValueObjectSymbolInspector.HasBinaryOperator(
			typeSymbol,
			"op_Equality",
			[scalarProperty.Type, typeSymbol]
		);
		var reversePrimitiveInequalityOperatorExists = ValueObjectSymbolInspector.HasBinaryOperator(
			typeSymbol,
			"op_Inequality",
			[scalarProperty.Type, typeSymbol]
		);
		var enumPropertiesEnabled =
			scalarOptions.GenerateEnumProperties && scalarProperty.Type.TypeKind == TypeKind.Enum;
		var toStringExists = ValueObjectSymbolInspector.HasParameterlessMethod(
			typeSymbol,
			"ToString"
		);
		var hasJsonConverterAttribute = ValueObjectSymbolInspector.HasAttribute(
			typeSymbol,
			ValueObjectSymbolInspector.JsonConverterAttributeName
		);
		var declareOnNormalize = ValueObjectSymbolInspector.ShouldEmitScalarHookDeclaration(
			typeSymbol,
			"OnNormalize",
			1,
			includeRef: true
		);
		var declareOnValidate = ValueObjectSymbolInspector.ShouldEmitScalarHookDeclaration(
			typeSymbol,
			"OnValidate",
			1,
			includeRef: false
		);

		if (
			scalarOptions.DeserializationMode == ValueObjectSymbolInspector.StrictModeName
			&& !createExists
		)
		{
			diagnosticsList.Add(
				DiagnosticInfo.Create(
					GeneratorDiagnostics.StrictDeserializationRequiresCreate,
					typeSymbol.Locations.FirstOrDefault(),
					typeSymbol.Name
				)
			);
		}

		var hintName = ValueObjectSymbolInspector.BuildHintName(typeSymbol, "ScalarValueObject");

		diagnostics = [.. diagnosticsList];
		return new ScalarValueObjectModel(
			typeSymbol,
			typeModel.Value,
			scalarProperty,
			scalarOptions,
			ctorExists,
			hintName,
			typeName,
			scalarTypeName,
			compareParameterTypeName,
			compareToSelfParameterTypeName,
			scalarCanBeNull,
			isReferenceType,
			scalarPropertyName,
			createExists,
			hydrateExists,
			tryCreateExists,
			compareToSelfExists,
			compareToPrimitiveExists,
			compareToObjectExists,
			equalsSelfExists,
			equalsPrimitiveExists,
			equalsObjectExists,
			getHashCodeExists,
			sameTypeEqualityOperatorExists,
			sameTypeInequalityOperatorExists,
			primitiveEqualityOperatorExists,
			primitiveInequalityOperatorExists,
			reversePrimitiveEqualityOperatorExists,
			reversePrimitiveInequalityOperatorExists,
			enumPropertiesEnabled,
			toStringExists,
			hasJsonConverterAttribute,
			declareOnNormalize,
			declareOnValidate
		);
	}
}
