using Microsoft.CodeAnalysis;

using Purview.EventSourcing.SourceGenerator.Helpers;
using Purview.SourceGeneratorFramework.Extensions;

using System.Collections.Immutable;

namespace Purview.EventSourcing.SourceGenerator.Models;

readonly record struct GenerationAggregateAttributeData(
	bool Exists,
	string? EventNamespace,
	string? EventSuffix)
{
	public static readonly GenerationAggregateAttributeData Empty = new(
	false,
	null,
	null
);

	public static GenerationAggregateAttributeData FromAttributeData(
	Compilation compilation,
	ImmutableArray<AttributeData> attributeData
)
	{
		var attributeSymbol = compilation.GetTypeByMetadataName(
			TypeLibrary.Attributes.GenerateAggregateAttribute
		);

		if (
			attributeSymbol is not null
		)
		{
			for (var i = 0; i < attributeData.Length; i++)
			{
				var result = FromAttributeData(
					attributeSymbol,
					attributeData[i]
				);

				if (result.Exists)
					return result;
			}
		}

		return Empty;
	}

	public static GenerationAggregateAttributeData FromAttributeData(
	INamedTypeSymbol? attributeSymbol,
	AttributeData attributeData
)
	{
		var exists =
			attributeSymbol is not null
			&& SymbolEqualityComparer.Default.Equals(
				attributeData?.AttributeClass,
				attributeSymbol
			);
		var eventNamespace = (string?)null;
		var eventSuffix = (string?)null;

		if (exists)
			(eventNamespace, eventSuffix) = ReadAttributeArguments(attributeData!);

		return new GenerationAggregateAttributeData(
			exists,
			eventNamespace,
			eventSuffix
		);
	}

	static (string? EventNamespace, string? EventSuffix) ReadAttributeArguments(
	AttributeData attributeData
)
	{
		string? eventNamespace;
		string? eventSuffix;
		if (!attributeData.TryGetConstructorArgument(nameof(eventNamespace), out eventNamespace))
			eventNamespace = attributeData.GetNamedArgument<string>(nameof(EventNamespace));

		if (
			!attributeData.TryGetConstructorArgument(
				nameof(eventSuffix),
				out eventSuffix
			)
		)
			eventSuffix = attributeData.GetNamedArgument<string>(
				nameof(EventSuffix)
			);

		return (eventNamespace, eventSuffix);
	}
}
