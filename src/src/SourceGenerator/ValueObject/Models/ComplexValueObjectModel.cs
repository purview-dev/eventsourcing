namespace Purview.EventSourcing.SourceGenerator.ValueObject.Models;

sealed record class ComplexValueObjectModel(
	GeneratedTypeModel TypeModel,
	EquatableArray<ComplexPropertyModel> Properties,
	ValueObjectAttributeData Options,
	bool CtorExists,
	string HintName,
	string TypeName,
	bool IsReferenceType,
	bool IsStruct,
	bool IsRecord,
	bool IsReadOnly,
	TypeDeclarationAccessibility? Accessibility,
	bool ImplementsSelfEquatable,
	bool HydrateExists,
	bool CreateExists,
	bool CompareToSelfExists,
	bool CompareToObjectExists,
	bool EqualsSelfExists,
	bool EqualsObjectExists,
	bool GetHashCodeExists,
	bool EqualityOperatorExists,
	bool InequalityOperatorExists,
	bool HasJsonConverterAttribute,
	bool DeclareOnNormalize,
	bool DeclareOnValidate,
	bool ValidateHookIsReadOnly,
	bool EmptyExists,
	EquatableArray<string> EmptyArguments,
	bool ParameterlessCtorExists,
	string? EfConstructorArguments,
	string HydrateFactoryName,
	EquatableArray<string> ExistingRelationalOperators
);

readonly record struct ComplexPropertyModel(string Name, string TypeName, TypeReference Type);
