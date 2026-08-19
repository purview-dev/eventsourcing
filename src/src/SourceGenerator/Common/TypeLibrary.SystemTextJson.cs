namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class TypeLibrary
{
	static partial class System
	{
		public static class TextJson
		{
			public static readonly TypeValueObject JsonConverterAttribute = new(
				nameof(JsonConverterAttribute),
				"System.Text.Json.Serialization"
			);
		}
	}
}
