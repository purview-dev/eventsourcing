using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.ValueObject.Models;

sealed record class ComplexValueObjectModel(
	INamedTypeSymbol TypeSymbol,
	GeneratedTypeModel TypeModel,
	IPropertySymbol[] Properties,
	ValueObjectAttributeData Options,
	bool CtorExists,
	string HintName,
	string TypeName,
	string[] PropertyTypeNames,
	string[] PropertyNames,
	bool HydrateExists,
	bool CompareToSelfExists,
	bool CompareToObjectExists,
	bool IsReferenceType,
	string CompareToSelfParameterTypeName,
	bool EqualsSelfExists,
	bool EqualsObjectExists,
	bool GetHashCodeExists,
	bool EqualityOperatorExists,
	bool InequalityOperatorExists,
	bool HasJsonConverterAttribute,
	bool CreateExists,
	bool DeclareOnNormalize,
	bool ParameterlessCtorExists,
	string HydrateFactoryName
);
