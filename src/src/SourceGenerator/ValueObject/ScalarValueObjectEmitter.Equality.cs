namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ScalarValueObjectEmitter
{
	static void EmitEquality(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (!model.EqualsSelfExists)
		{
			ValueObjectEmitterHelpers.WriteExpressionMethod(
				writer,
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("other", ValueObjectType(model))],
					ExpressionBody = model.IsReferenceType
						? $"other is not null && global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other.{model.ScalarPropertyName})"
						: $"global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other.{model.ScalarPropertyName})",
				}
			);
		}

		if (!model.EqualsPrimitiveExists)
		{
			ValueObjectEmitterHelpers.WriteExpressionMethod(
				writer,
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("other", model.ScalarTypeReference)],
					ExpressionBody =
						$"global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other)",
				}
			);
		}

		if (!model.EqualsObjectExists)
		{
			ValueObjectEmitterHelpers.WriteExpressionMethod(
				writer,
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
					Parameters = [new("obj", PurviewTypeLibrary.System.Object.AsTypeReference().Nullable())],
					ExpressionBody =
						$"obj is {model.TypeName} other ? Equals(other) : obj is {model.ScalarTypeName} primitive && Equals(primitive)",
				}
			);
		}

		if (!model.GetHashCodeExists)
		{
			ValueObjectEmitterHelpers.WriteExpressionMethod(
				writer,
				new("GetHashCode", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
					ExpressionBody =
						$"global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.GetHashCode({model.ScalarPropertyName})",
				}
			);
		}
	}

	static void EmitOperators(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (!model.SameTypeEqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.TypeName,
				model.TypeName,
				"==",
				model.IsReferenceType ? "left is null ? right is null : left.Equals(right)" : "left.Equals(right)"
			);
		}

		if (!model.SameTypeInequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.TypeName,
				model.TypeName,
				"!=",
				"!(left == right)"
			);
		}

		if (!model.PrimitiveEqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.TypeName,
				model.ScalarTypeName,
				"==",
				model.IsReferenceType ? "left is null ? false : left.Equals(right)" : "left.Equals(right)"
			);
		}

		if (!model.PrimitiveInequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.TypeName,
				model.ScalarTypeName,
				"!=",
				"!(left == right)"
			);
		}

		if (!model.ReversePrimitiveEqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.ScalarTypeName,
				model.TypeName,
				"==",
				model.IsReferenceType ? "right is not null && right.Equals(left)" : "right.Equals(left)"
			);
		}

		if (!model.ReversePrimitiveInequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.ScalarTypeName,
				model.TypeName,
				"!=",
				"!(left == right)"
			);
		}
	}

	static void EmitConversions(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (model.Options.GenerateImplicitToPrimitive && !model.HasValueObjectToPrimitiveConversion)
		{
			writer
				.Write(
					$"public static implicit operator {model.ScalarTypeName}({model.TypeName} valueObject) => valueObject.{model.ScalarPropertyName};"
				)
				.NewLine();
		}

		if (
			model.Options.GenerateImplicitFromPrimitive
			&& !model.HasContextualCreateOverload
			&& !model.HasPrimitiveToValueObjectConversion
		)
		{
			writer
				.Write(
					$"public static implicit operator {model.TypeName}({model.ScalarTypeName} value) => Create(value);"
				)
				.NewLine();
		}
	}

	static void EmitToString(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (model.ToStringExists)
			return;

		ValueObjectEmitterHelpers.WriteExpressionMethod(
			writer,
			new("ToString", PurviewTypeLibrary.System.String, TypeDeclarationAccessibility.Public)
			{
				IsOverride = true,
				ExpressionBody = $"{model.ScalarPropertyName}.ToString() ?? string.Empty",
			}
		);
	}

	static void EmitJsonConverter(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (!model.Options.GenerateJsonConverter)
			return;

		var valueObjectType = ValueObjectType(model);
		var factoryMethod =
			model.Options.DeserializationMode == ValueObjectSymbolInspector.StrictModeName ? "Create" : "Hydrate";

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
							"value",
							writeValue =>
								writeValue.WriteMethodCall(
									TypeLibrary.System.TextJson.JsonSerializer.StaticMember("Deserialize"),
									[new MethodCallArgumentOptions("reader", ParameterModifier.Ref), "options"],
									genericArguments: [model.ScalarTypeReference]
								)
						);

						if (model.ScalarCanBeNull)
						{
							methodBody.WriteIfBlock(
								"value is null",
								ifBody =>
									ifBody.WriteThrow(
										$"new {TypeLibrary.System.TextJson.JsonException}(\"{model.TypeModel.Name} cannot be null.\")"
									)
							);
						}

						methodBody.WriteReturn($"{factoryMethod}(value)");
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
						methodBody.WriteMethodCall(
							TypeLibrary.System.TextJson.JsonSerializer.StaticMember("Serialize"),
							["writer", $"value.{model.ScalarPropertyName}", "options"]
						)
				);
			}
		);
	}
}
