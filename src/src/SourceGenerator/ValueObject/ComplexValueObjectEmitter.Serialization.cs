namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ComplexValueObjectEmitter
{
	static void EmitJsonConverter(CodeWriter writer, ComplexValueObjectModel model)
	{
		if (!model.Options.GenerateJsonConverter)
			return;

		var modelTypeName = $"{model.TypeModel.Name}JsonModel";
		var toModelAssignments = string.Join(
			", ",
			model.Properties.Select(static property => $"{property.Name} = value.{property.Name}")
		);
		var hydrateArgs = string.Join(", ", model.Properties.Select(static property => $"model.{property.Name}"));
		var valueObjectType = ValueObjectType(model);

		writer.WriteClass(
			new($"{model.TypeModel.Name}JsonConverter")
			{
				IsPartial = false,
				IsSealed = true,
				BaseType = TypeLibrary.System.TextJson.JsonConverter.MakeGeneric(valueObjectType),
			},
			body =>
			{
				body.WriteMethod(
					new("Read", valueObjectType, TypeDeclarationAccessibility.Public)
					{
						IsOverride = true,
						Parameters =
						[
							new("reader", TypeLibrary.System.TextJson.Utf8JsonReader, ParameterModifier.Ref),
							new("typeToConvert", PurviewTypeLibrary.System.Type),
							new("options", TypeLibrary.System.TextJson.JsonSerializerOptions),
						],
					},
					methodBody =>
					{
						methodBody.WriteAssignment(
							"var",
							"model",
							$"global::System.Text.Json.JsonSerializer.Deserialize<{modelTypeName}>(ref reader, options)"
						);
						methodBody.WriteIfBlock(
							"model is null",
							ifBody =>
								ifBody.WriteThrow(
									$"new {TypeLibrary.System.TextJson.JsonException}(\"Unable to deserialize {model.TypeModel.Name}.\")"
								)
						);
						methodBody.WriteReturn($"{model.HydrateFactoryName}({hydrateArgs})");
					}
				);

				body.WriteMethod(
					new("Write", TypeDeclarationAccessibility.Public)
					{
						IsOverride = true,
						Parameters =
						[
							new("writer", TypeLibrary.System.TextJson.Utf8JsonWriter),
							new("value", valueObjectType),
							new("options", TypeLibrary.System.TextJson.JsonSerializerOptions),
						],
					},
					methodBody =>
					{
						methodBody.WriteAssignment("var", "model", $"new {modelTypeName} {{ {toModelAssignments} }}");
						methodBody.WriteMethodCall(
							TypeLibrary.System.TextJson.JsonSerializer.StaticMember("Serialize"),
							["writer", "model", "options"]
						);
					}
				);
			}
		);

		writer.WriteClass(
			new(modelTypeName) { IsPartial = false, IsSealed = true },
			body =>
			{
				foreach (var property in model.Properties)
				{
					body.WriteProperty(
						new(property.Name, property.Type, TypeDeclarationAccessibility.Public)
						{
							HasSetter = true,
							Initializer = "default!",
						}
					);
				}
			}
		);
	}
}
