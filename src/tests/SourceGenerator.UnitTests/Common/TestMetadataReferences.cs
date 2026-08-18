using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Common;

public static class TestMetadataReferences
{
	public static IReadOnlyList<MetadataReference> GetAdditionalReferences()
	{
		var references = new List<MetadataReference>
		{
			MetadataReference.CreateFromFile(
				typeof(System.Text.Json.JsonSerializer).Assembly.Location
			),
			MetadataReference.CreateFromFile(
				typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location
			),
		};

		return references;
	}
}
