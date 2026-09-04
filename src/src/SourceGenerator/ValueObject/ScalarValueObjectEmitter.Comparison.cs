namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ScalarValueObjectEmitter
{
	static void EmitComparison(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (!model.CompareToSelfExists)
		{
			writer.Method(
				new("CompareTo", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						new(
							"other",
							model.IsReferenceType ? ValueObjectType(model).Nullable(writer) : ValueObjectType(model)
						),
					],
				},
				body =>
				{
					if (model.IsReferenceType)
						body.IfBlock("other is null", ifBody => ifBody.Return("1"));

					body.Return($"CompareTo(other.{model.ScalarPropertyName})");
				}
			);
		}

		if (!model.CompareToPrimitiveExists)
		{
			writer.MethodExpression(
				new("CompareTo", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						new(
							"other",
							model.ScalarIsReferenceType
								? model.ScalarTypeReference.Nullable(writer)
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
			writer.Method(
				new("CompareTo", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("obj", PurviewTypeLibrary.System.Object.MakeNullable(writer))],
				},
				body =>
				{
					body.IfBlock("obj is null", ifBody => ifBody.Return("1"));
					body.IfBlock(
						$"obj is {model.TypeName} otherValueObject",
						ifBody => ifBody.Return("CompareTo(otherValueObject)")
					);
					body.IfBlock(
						$"obj is {model.ScalarTypeName} primitive",
						ifBody => ifBody.Return("CompareTo(primitive)")
					);
					body.Throw(
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
				ValueObjectType(model),
				ValueObjectType(model),
				"CompareTo(right)"
			);

			if (!model.ScalarAndSelfAreSameType)
			{
				ValueObjectEmitterHelpers.EmitRelationalOperators(
					writer,
					model.ExistingScalarRelationalOperators,
					ValueObjectType(model),
					model.ScalarTypeReference,
					"CompareTo(right)"
				);
			}
		}
	}
}
