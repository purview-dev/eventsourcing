using System.Text;

namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ScalarValueObjectEmitter
{
	static void EmitEquality(StringBuilder sb, ScalarValueObjectModel model, string indent)
	{
		if (!model.EqualsSelfExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				model.IsReferenceType
					? $"{indent}	public bool Equals({model.TypeName} other) => other is not null && global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other.{model.ScalarPropertyName});"
					: $"{indent}	public bool Equals({model.TypeName} other) => global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other.{model.ScalarPropertyName});"
			);
		}

		if (!model.EqualsPrimitiveExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$"{indent}	public bool Equals({model.ScalarTypeName} other) => global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other);"
			);
		}

		if (!model.EqualsObjectExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$@"{indent}	public override bool Equals(object? obj) =>
{indent}		obj is {model.TypeName} other ? Equals(other) : obj is {model.ScalarTypeName} primitive && Equals(primitive);"
			);
		}

		if (!model.GetHashCodeExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$"{indent}	public override int GetHashCode() => global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.GetHashCode({model.ScalarPropertyName});"
			);
		}
	}

	static void EmitOperators(StringBuilder sb, ScalarValueObjectModel model, string indent)
	{
		if (!model.SameTypeEqualityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				model.IsReferenceType
					? $"{indent}	public static bool operator ==({model.TypeName} left, {model.TypeName} right) =>\n{indent}		left is null ? right is null : left.Equals(right);"
					: $"{indent}	public static bool operator ==({model.TypeName} left, {model.TypeName} right) =>\n{indent}		left.Equals(right);"
			);
		}

		if (!model.SameTypeInequalityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$"{indent}	public static bool operator !=({model.TypeName} left, {model.TypeName} right) =>\n{indent}		!(left == right);"
			);
		}

		if (!model.PrimitiveEqualityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				model.IsReferenceType
					? $"{indent}	public static bool operator ==({model.TypeName} left, {model.ScalarTypeName} right) =>\n{indent}		left is null ? false : left.Equals(right);"
					: $"{indent}	public static bool operator ==({model.TypeName} left, {model.ScalarTypeName} right) =>\n{indent}		left.Equals(right);"
			);
		}

		if (!model.PrimitiveInequalityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$"{indent}	public static bool operator !=({model.TypeName} left, {model.ScalarTypeName} right) =>\n{indent}		!(left == right);"
			);
		}

		if (!model.ReversePrimitiveEqualityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				model.IsReferenceType
					? $"{indent}	public static bool operator ==({model.ScalarTypeName} left, {model.TypeName} right) =>\n{indent}		right is not null && right.Equals(left);"
					: $"{indent}	public static bool operator ==({model.ScalarTypeName} left, {model.TypeName} right) =>\n{indent}		right.Equals(left);"
			);
		}

		if (!model.ReversePrimitiveInequalityOperatorExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$"{indent}	public static bool operator !=({model.ScalarTypeName} left, {model.TypeName} right) =>\n{indent}		!(left == right);"
			);
		}
	}

	static void EmitConversions(StringBuilder sb, ScalarValueObjectModel model, string indent)
	{
		if (
			model.Options.GenerateImplicitToPrimitive
			&& !ValueObjectSymbolInspector.HasConversionOperator(
				model.TypeSymbol,
				model.ScalarProperty.Type,
				false
			)
		)
		{
			sb.AppendLine(
				$"{indent}	public static implicit operator {model.ScalarTypeName}({model.TypeName} valueObject) => valueObject.{model.ScalarPropertyName};"
			);
		}

		if (
			model.Options.GenerateImplicitFromPrimitive
			&& !ValueObjectSymbolInspector.HasContextualCreateOverload(
				model.TypeSymbol,
				model.ScalarProperty.Type
			)
			&& !ValueObjectSymbolInspector.HasConversionOperator(
				model.TypeSymbol,
				model.ScalarProperty.Type,
				true
			)
		)
		{
			sb.AppendLine(
				$"{indent}	public static implicit operator {model.TypeName}({model.ScalarTypeName} value) => Create(value);"
			);
		}
	}

	static void EmitToString(StringBuilder sb, ScalarValueObjectModel model, string indent)
	{
		if (!model.ToStringExists)
		{
			sb.AppendLine();
			sb.AppendLine(
				$"{indent}	public override string ToString() => {model.ScalarPropertyName}.ToString() ?? string.Empty;"
			);
		}
	}

	static void EmitJsonConverter(StringBuilder sb, ScalarValueObjectModel model, string indent)
	{
		if (!model.Options.GenerateJsonConverter)
			return;

		sb.AppendLine();
		sb.AppendLine(
			$@"{indent}	sealed class {model.TypeModel.Name}JsonConverter : global::System.Text.Json.Serialization.JsonConverter<{model.TypeName}>
{indent}	{{
{indent}		public override {model.TypeName} Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
{indent}		{{
{indent}			var value = global::System.Text.Json.JsonSerializer.Deserialize<{model.ScalarTypeName}>(ref reader, options);
{(model.ScalarCanBeNull ? $"{indent}			if (value is null)\n{indent}				throw new global::System.Text.Json.JsonException(\"{model.TypeModel.Name} cannot be null.\");\n" : string.Empty)}{indent}			return {(model.Options.DeserializationMode == ValueObjectSymbolInspector.StrictModeName ? "Create" : "Hydrate")}(value);
{indent}		}}

{indent}		public override void Write(global::System.Text.Json.Utf8JsonWriter writer, {model.TypeName} value, global::System.Text.Json.JsonSerializerOptions options) =>
{indent}			global::System.Text.Json.JsonSerializer.Serialize(writer, value.{model.ScalarPropertyName}, options);
{indent}	}}"
		);
	}
}
