using System.Text;

namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ComplexValueObjectEmitter
{
	static void EmitEquality(StringBuilder sb, ComplexValueObjectModel model, string indent)
	{
		if (!model.EqualsSelfExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				model.IsReferenceType
					? $@"{indent}	public bool Equals({model.TypeName} other)
{indent}	{{
{indent}		if (other is null)
{indent}			return false;"
					: $"{indent}	public bool Equals({model.TypeName} other)\n{indent}	{{"
			);
			for (var i = 0; i < model.Properties.Length; i++)
			{
				sb.AppendLine(
					$"{indent}		if (!global::System.Collections.Generic.EqualityComparer<{model.PropertyTypeNames[i]}>.Default.Equals({model.PropertyNames[i]}, other.{model.PropertyNames[i]}))"
				);
				sb.AppendLine($"{indent}			return false;");
			}

			sb.AppendLine($"{indent}		return true;");
			sb.AppendLine($"{indent}	}}");
		}

		if (!model.EqualsObjectExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$@"{indent}	public override bool Equals(object? obj) =>
{indent}		obj is {model.TypeName} other && Equals(other);"
			);
		}

		if (!model.GetHashCodeExists)
		{
			sb.AppendLine();
			sb.AppendLine($"{indent}	public override int GetHashCode()");
			sb.AppendLine($"{indent}	{{");
			sb.AppendLine($"{indent}		global::System.HashCode hash = new();");
			for (var i = 0; i < model.PropertyNames.Length; i++)
				sb.AppendLine($"{indent}		hash.Add({model.PropertyNames[i]});");
			sb.AppendLine($"{indent}		return hash.ToHashCode();");
			sb.AppendLine($"{indent}	}}");
		}
	}

	static void EmitOperators(StringBuilder sb, ComplexValueObjectModel model, string indent)
	{
		if (!model.EqualityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				model.IsReferenceType
					? $"{indent}	public static bool operator ==({model.TypeName} left, {model.TypeName} right) =>\n{indent}		left is null ? right is null : left.Equals(right);"
					: $"{indent}	public static bool operator ==({model.TypeName} left, {model.TypeName} right) =>\n{indent}		left.Equals(right);"
			);
		}

		if (!model.InequalityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$"{indent}	public static bool operator !=({model.TypeName} left, {model.TypeName} right) =>\n{indent}		!(left == right);"
			);
		}
	}

	static void EmitComparison(StringBuilder sb, ComplexValueObjectModel model, string indent)
	{
		if (!model.CompareToSelfExists)
		{
			sb.AppendLine(
				$@"{indent}	public int CompareTo({model.CompareToSelfParameterTypeName} other)
{indent}	{{"
			);
			if (model.TypeSymbol.TypeKind == TypeKind.Class)
			{
				sb.AppendLine(
					$@"{indent}		if (other is null)
{indent}			return 1;"
				);
			}

			for (var i = 0; i < model.Properties.Length; i++)
			{
				var prop = model.Properties[i];
				var propTypeName = model.PropertyTypeNames[i];
				sb.AppendLine(
					$@"{indent}		var compare{prop.Name} = global::System.Collections.Generic.Comparer<{propTypeName}>.Default.Compare({prop.Name}, other.{prop.Name});
{indent}		if (compare{prop.Name} != 0)
{indent}			return compare{prop.Name};"
				);
			}

			sb.AppendLine(
				$@"{indent}		return 0;
{indent}	}}"
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
{indent}		throw new global::System.ArgumentException($""Object must be of type {{nameof({model.TypeModel.Name})}}."", nameof(obj));
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
		}
	}
}
