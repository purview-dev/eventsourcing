using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Purview.EventSourcing.SourceGenerator.Helpers;

using System.Globalization;

namespace Purview.EventSourcing.SourceGenerator.Extensions.Purview.SourceGeneratorFramework.Helpers;

static class TypeHelperExtensions
{
	const int HintNameHashHexLength = 16;
	const string GeneratedSourceFileSuffix = ".g.cs";
	static readonly int HintNameSeparatorAndSuffixLength = 1 + HintNameHashHexLength + GeneratedSourceFileSuffix.Length;


	extension(TypeHelpers)
	{
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
				&& namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
			)
				return true;

			foreach (var interfaceSymbol in namedType.AllInterfaces)
			{
				if (
					interfaceSymbol is INamedTypeSymbol namedInterface
					&& namedInterface.IsGenericType
					&& namedInterface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
				)
					return true;
			}

			return false;
		}

		public static bool TryGetCollectionDetails(ITypeSymbol typeSymbol, out ITypeSymbol elementType, out bool isSet)
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

		public static bool TryGetIEnumerableElementType(ITypeSymbol typeSymbol, out ITypeSymbol elementType)
		{
			elementType = null!;

			if (
				typeSymbol is INamedTypeSymbol namedType
				&& namedType.IsGenericType
				&& namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
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
			&& (TypeLibrary.Aggregates.EventStoreList.Equals(namedType.OriginalDefinition) || TypeLibrary.Aggregates.EventStoreSet.Equals(namedType.OriginalDefinition));

		public static bool TryGetComplexScalarValueType(ITypeSymbol typeSymbol, out string valueTypeDisplayName)
		{
			valueTypeDisplayName = string.Empty;

			if (typeSymbol is not INamedTypeSymbol namedType)
				return false;

			if (!HasAttribute(namedType, TypeLibrary.Attributes.ScalarAttribute))
				return false;

			var valueProperty = namedType
				.GetMembers("Value")
				.OfType<IPropertySymbol>()
				.FirstOrDefault(static property => property.GetMethod is not null && !property.IsStatic);

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

			if (typeSymbol is not INamedTypeSymbol namedType)
				return false;

			var fullyQualifiedName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			return fullyQualifiedName
				is "global::System.Guid"
					or "global::System.DateTime"
					or "global::System.DateTimeOffset"
					or "global::System.TimeSpan"
					or "global::System.DateOnly"
					or "global::System.TimeOnly";
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
			symbol.GetAttributes().Any(attributeType.Equals);

		public static bool HasComputedAttribute(IParameterSymbol parameterSymbol)
		{
			foreach (var attribute in parameterSymbol.GetAttributes())
			{
				var attributeClass = attribute.AttributeClass;
				if (attributeClass is not null && TypeLibrary.Attributes.ComputedAttribute.Equals(attributeClass))
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
				if (attributeClass is null || !TypeLibrary.Attributes.AggregatePropertyAttribute.Equals(attributeClass))
					continue;

				if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string value)
					return value.Trim();

				break;
			}

			return null;
		}

		public static bool TryGetMetadataStoreSetting(IParameterSymbol parameterSymbol, out bool storeMetadata)
		{
			storeMetadata = true;

			foreach (var attribute in parameterSymbol.GetAttributes())
			{
				var attributeClass = attribute.AttributeClass;
				if (attributeClass is null || !TypeLibrary.Attributes.MetadataAttribute.Equals(attributeClass))
					continue;

				if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is bool value)
					storeMetadata = value;

				return true;
			}

			return false;
		}

		public static bool HasRegisterEventsMethod(INamedTypeSymbol classSymbol, out IMethodSymbol? registerEventsMethod)
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
		out string returnTypeName,
		out EventMethodReturnKind returnKind
	)
		{
			returnTypeName = "void";
			returnKind = EventMethodReturnKind.Void;

			if (methodSymbol.ReturnsVoid)
				return true;

			if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean)
			{
				returnTypeName = "bool";
				returnKind = EventMethodReturnKind.Bool;
				return true;
			}

			if (SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, classSymbol))
			{
				returnTypeName = classSymbol.Name;
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

			var modifiers = string.Join(" ", declaration.Modifiers.Select(static modifier => modifier.Text));
			if (modifiers.Length > 0)
				modifiers += " ";

			var explicitInterfaceSpecifier = declaration.ExplicitInterfaceSpecifier?.ToString() ?? string.Empty;
			var typeParameterList = declaration.TypeParameterList?.ToString() ?? string.Empty;
			var constraints =
				declaration.ConstraintClauses.Count == 0
					? string.Empty
					: " " + string.Join(" ", declaration.ConstraintClauses.Select(static clause => clause.ToString()));

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
			var builder = new System.Text.StringBuilder(shortName.Length + HintNameSeparatorAndSuffixLength);

			foreach (var character in shortName)
			{
				builder.Append(char.IsLetterOrDigit(character) ? character : '_');
			}

			builder.Append('_');
			builder.Append(
				ComputeStableHash(symbolName).ToString($"X{HintNameHashHexLength}", CultureInfo.InvariantCulture)
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
	}
}
