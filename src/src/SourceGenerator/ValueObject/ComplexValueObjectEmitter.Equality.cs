namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ComplexValueObjectEmitter
{
	static void EmitEquality(CodeWriter writer, ComplexValueObjectModel model)
	{
		if (!model.EqualsSelfExists)
		{
			writer.WriteMethod(
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("other", ValueObjectType(model))],
				},
				body =>
				{
					if (model.IsReferenceType)
						body.WriteIfBlock("other is null", ifBody => ifBody.WriteReturn("false"));

					foreach (var property in model.Properties)
					{
						body.WriteIfBlock(
							$"!global::System.Collections.Generic.EqualityComparer<{property.TypeName}>.Default.Equals({property.Name}, other.{property.Name})",
							ifBody => ifBody.WriteReturn("false")
						);
					}

					body.WriteReturn("true");
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
					ExpressionBody = $"obj is {model.TypeName} other && Equals(other)",
				}
			);
		}

		if (!model.GetHashCodeExists)
		{
			writer.WriteMethod(
				new("GetHashCode", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
				},
				body =>
				{
					body.WriteAssignment("var", "hash", "new global::System.HashCode()");
					foreach (var property in model.Properties)
						body.WriteMethodCall("hash.Add", property.Name);

					body.WriteReturn("hash.ToHashCode()");
				}
			);
		}
	}

	static void EmitOperators(CodeWriter writer, ComplexValueObjectModel model)
	{
		if (!model.EqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.TypeName,
				model.TypeName,
				"==",
				model.IsReferenceType ? "left is null ? right is null : left.Equals(right)" : "left.Equals(right)"
			);
		}

		if (!model.InequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.TypeName,
				model.TypeName,
				"!=",
				"!(left == right)"
			);
		}
	}

	static void EmitComparison(CodeWriter writer, ComplexValueObjectModel model)
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

					foreach (var property in model.Properties)
					{
						body.WriteAssignment(
							"var",
							$"compare{property.Name}",
							$"global::System.Collections.Generic.Comparer<{property.TypeName}>.Default.Compare({property.Name}, other.{property.Name})"
						);
						body.WriteIfBlock(
							$"compare{property.Name} != 0",
							ifBody => ifBody.WriteReturn($"compare{property.Name}")
						);
					}

					body.WriteReturn("0");
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
					body.WriteThrow(
						$"new global::System.ArgumentException($\"Object must be of type {{nameof({model.TypeModel.Name})}}.\", nameof(obj))"
					);
				}
			);
		}

		if (model.Options.GenerateComparable && model.Options.GenerateComparisonOperators)
		{
			ValueObjectEmitterHelpers.EmitRelationalOperators(
				writer,
				model.ExistingRelationalOperators,
				model.TypeName,
				model.TypeName,
				"CompareTo(right)"
			);
		}
	}
}
