namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ScalarValueObjectEmitter
{
	static void EmitComparison(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (!model.CompareToSelfExists)
		{
			writer.WriteMethod(
				new("CompareTo", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						new(
							"other",
							model.IsReferenceType ? ValueObjectType(model).Nullable() : ValueObjectType(model)
						),
					],
				},
				body =>
				{
					if (model.IsReferenceType)
						body.WriteIfBlock("other is null", ifBody => ifBody.WriteReturn("1"));

					body.WriteReturn($"CompareTo(other.{model.ScalarPropertyName})");
				}
			);
		}

		if (!model.CompareToPrimitiveExists)
		{
			ValueObjectEmitterHelpers.WriteExpressionMethod(
				writer,
				new("CompareTo", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						new(
							"other",
							model.ScalarIsReferenceType
								? model.ScalarTypeReference.Nullable()
								: model.ScalarTypeReference
						),
					],
					ExpressionBody =
						$"global::System.Collections.Generic.Comparer<{model.ScalarTypeName}>.Default.Compare({model.ScalarPropertyName}, other)",
				}
			);
		}

		if (!model.CompareToObjectExists)
		{
			writer.WriteMethod(
				new("CompareTo", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("obj", PurviewTypeLibrary.System.Object.AsTypeReference().Nullable())],
				},
				body =>
				{
					body.WriteIfBlock("obj is null", ifBody => ifBody.WriteReturn("1"));
					body.WriteIfBlock(
						$"obj is {model.TypeName} otherValueObject",
						ifBody => ifBody.WriteReturn("CompareTo(otherValueObject)")
					);
					body.WriteIfBlock(
						$"obj is {model.ScalarTypeName} primitive",
						ifBody => ifBody.WriteReturn("CompareTo(primitive)")
					);
					body.WriteThrow(
						$"new global::System.ArgumentException($\"Object must be of type {{nameof({model.TypeModel.Name})}} or {model.ScalarTypeName}.\", nameof(obj))"
					);
				}
			);
		}

		if (model.Options.GenerateComparable && model.Options.GenerateComparisonOperators)
		{
			ValueObjectEmitterHelpers.EmitRelationalOperators(
				writer,
				model.ExistingSelfRelationalOperators,
				model.TypeName,
				model.TypeName,
				"CompareTo(right)"
			);

			if (!model.ScalarAndSelfAreSameType)
			{
				ValueObjectEmitterHelpers.EmitRelationalOperators(
					writer,
					model.ExistingScalarRelationalOperators,
					model.TypeName,
					model.ScalarTypeName,
					"CompareTo(right)"
				);
			}
		}
	}
}
