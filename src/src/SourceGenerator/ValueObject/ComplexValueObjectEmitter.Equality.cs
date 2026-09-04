namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ComplexValueObjectEmitter
{
	static void EmitEquality(CodeWriter writer, ComplexValueObjectModel model)
	{
		if (!model.EqualsSelfExists)
		{
			writer.Method(
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("other", ValueObjectType(model))],
				},
				body =>
				{
					if (model.IsReferenceType)
						body.IfBlock("other is null", ifBody => ifBody.Return("false"));

					foreach (var property in model.Properties)
					{
						body.IfBlock(
							$"!global::System.Collections.Generic.EqualityComparer<{property.TypeName}>.Default.Equals({property.Name}, other.{property.Name})",
							ifBody => ifBody.Return("false")
						);
					}

					body.Return("true");
				}
			);
		}

		if (!model.EqualsObjectExists)
		{
			writer.MethodExpression(
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
					Parameters = [new("obj", PurviewTypeLibrary.System.Object.MakeNullable(writer))],
					ExpressionBody = $"obj is {model.TypeName} other && Equals(other)",
				}
			);
		}

		if (!model.GetHashCodeExists)
		{
			writer.Method(
				new("GetHashCode", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
				},
				body =>
				{
					body.Assignment("var", "hash", "new global::System.HashCode()");
					foreach (var property in model.Properties)
						body.MethodCallOn("hash", "Add", property.Name);

					body.Return("hash.ToHashCode()");
				}
			);
		}
	}

	static void EmitOperators(CodeWriter writer, ComplexValueObjectModel model)
	{
		var valueObjectType = ValueObjectType(model);

		if (!model.EqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				valueObjectType,
				valueObjectType,
				"==",
				model.IsReferenceType ? "left is null ? right is null : left.Equals(right)" : "left.Equals(right)"
			);
		}

		if (!model.InequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				valueObjectType,
				valueObjectType,
				"!=",
				"!(left == right)"
			);
		}
	}

	static void EmitComparison(CodeWriter writer, ComplexValueObjectModel model)
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

					foreach (var property in model.Properties)
					{
						body.Assignment(
							"var",
							$"compare{property.Name}",
							$"global::System.Collections.Generic.Comparer<{property.TypeName}>.Default.Compare({property.Name}, other.{property.Name})"
						);
						body.IfBlock(
							$"compare{property.Name} != 0",
							ifBody => ifBody.Return($"compare{property.Name}")
						);
					}

					body.Return("0");
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
					body.Throw(
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
				ValueObjectType(model),
				ValueObjectType(model),
				"CompareTo(right)"
			);
		}
	}
}
