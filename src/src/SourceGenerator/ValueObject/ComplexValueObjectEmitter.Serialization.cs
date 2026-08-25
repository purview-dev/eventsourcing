using System.Text;

namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ComplexValueObjectEmitter
{
	static void EmitJsonConverter(StringBuilder sb, ComplexValueObjectModel model, string indent)
	{
		if (!model.Options.GenerateJsonConverter)
			return;

		var modelTypeName = $"{model.TypeModel.Name}JsonModel";
		var toModelAssignments = string.Join(", ", model.PropertyNames.Select(static name => $"{name} = value.{name}"));
		var hydrateArgs = string.Join(", ", model.PropertyNames.Select(static name => $"model.{name}"));

		sb.AppendLine();
		sb.AppendLine(
			$@"{indent}	sealed class {model.TypeModel.Name}JsonConverter : global::System.Text.Json.Serialization.JsonConverter<{model.TypeName}>
{indent}	{{
{indent}		public override {model.TypeName} Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
{indent}		{{
{indent}			var model = global::System.Text.Json.JsonSerializer.Deserialize<{modelTypeName}>(ref reader, options);
{indent}			if (model is null)
{indent}				throw new global::System.Text.Json.JsonException(""Unable to deserialize {model.TypeModel.Name}."");
{indent}			return {model.HydrateFactoryName}({hydrateArgs});
{indent}		}}

{indent}		public override void Write(global::System.Text.Json.Utf8JsonWriter writer, {model.TypeName} value, global::System.Text.Json.JsonSerializerOptions options)
{indent}		{{
{indent}			var model = new {modelTypeName} {{ {toModelAssignments} }};
{indent}			global::System.Text.Json.JsonSerializer.Serialize(writer, model, options);
{indent}		}}
{indent}	}}

{indent}	sealed class {modelTypeName}
{indent}	{{"
		);

		for (var i = 0; i < model.Properties.Length; i++)
		{
			sb.AppendLine(
				$"{indent}		public {model.PropertyTypeNames[i]} {model.PropertyNames[i]} {{ get; set; }} = default!;"
			);
		}

		sb.AppendLine($"{indent}	}}");
	}
}
