namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class TypeLibrary
{
	static partial class System
	{
		public static class TextJson
		{
			const string Namespace = "System.Text.Json";
			const string SerializerNamespace = Namespace + ".Serialization";

			public static readonly TypeIdentity JsonConverterAttribute = new(
				nameof(JsonConverterAttribute),
				SerializerNamespace
			);

			public static readonly TypeIdentity JsonConverter = new(nameof(JsonConverter), SerializerNamespace);

			public static readonly TypeIdentity JsonSerializer = new(nameof(JsonSerializer), Namespace);

			public static readonly TypeIdentity JsonException = new(nameof(JsonException), Namespace);

			public static readonly TypeIdentity JsonSerializerOptions = new(nameof(JsonSerializerOptions), Namespace);

			public static readonly TypeIdentity Utf8JsonReader = new(nameof(Utf8JsonReader), Namespace);
			public static readonly TypeIdentity Utf8JsonWriter = new(nameof(Utf8JsonWriter), Namespace);
		}
	}
}
