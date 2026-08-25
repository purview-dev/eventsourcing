using System.Text;

namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ScalarValueObjectEmitter
{
	static void EmitComparison(StringBuilder sb, ScalarValueObjectModel model, string indent)
	{
		if (!model.CompareToSelfExists)
		{
			sb.AppendLine(
				model.TypeSymbol.TypeKind == TypeKind.Class
					? $@"{indent}	public int CompareTo({model.CompareToSelfParameterTypeName} other)
{indent}	{{
{indent}		if (other is null)
{indent}			return 1;
{indent}		return CompareTo(other.{model.ScalarPropertyName});
{indent}	}}"
					: $"{indent}	public int CompareTo({model.CompareToSelfParameterTypeName} other) => CompareTo(other.{model.ScalarPropertyName});"
			);
		}

		if (!model.CompareToPrimitiveExists)
		{
			sb.AppendLine(
				$"{indent}	public int CompareTo({model.CompareParameterTypeName} other) => global::System.Collections.Generic.Comparer<{model.ScalarTypeName}>.Default.Compare({model.ScalarPropertyName}, other);"
			);
		}

		if (!model.CompareToObjectExists)
		{
			sb.AppendLine(
				$@"{indent}	public int CompareTo(object? obj)
{indent}	{{
{indent}		if (obj is null)
{indent}			return 1;
{indent}		if (obj is {model.TypeName} otherValueObject)
{indent}			return CompareTo(otherValueObject);
{indent}		if (obj is {model.ScalarTypeName} primitive)
{indent}			return CompareTo(primitive);
{indent}		throw new global::System.ArgumentException($""Object must be of type {{nameof({model.TypeModel.Name})}} or {model.ScalarTypeName}."", nameof(obj));
{indent}	}}"
			);
		}

		if (model.Options.GenerateComparable && model.Options.GenerateComparisonOperators)
		{
			ValueObjectEmitterHelpers.EmitRelationalOperators(
				sb,
				indent,
				model.TypeSymbol,
				model.TypeName,
				model.TypeName,
				"CompareTo(right)"
			);

			var scalarAndSelfAreSameType = SymbolEqualityComparer.Default.Equals(
				model.ScalarProperty.Type,
				model.TypeSymbol
			);
			if (!scalarAndSelfAreSameType)
			{
				ValueObjectEmitterHelpers.EmitRelationalOperators(
					sb,
					indent,
					model.TypeSymbol,
					model.TypeName,
					model.ScalarTypeName,
					"CompareTo(right)"
				);
			}
		}

		sb.AppendLine();
	}
}
