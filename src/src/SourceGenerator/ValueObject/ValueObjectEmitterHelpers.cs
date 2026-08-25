using System.Text;

namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static class ValueObjectEmitterHelpers
{
	public static void EmitRelationalOperators(
		StringBuilder sb,
		string indent,
		INamedTypeSymbol declaringType,
		string leftTypeName,
		string rightTypeName,
		string compareExpression
	)
	{
		EmitRelationalOperator(
			sb,
			indent,
			declaringType,
			ValueObjectSymbolInspector.LessThanOperatorName,
			"<",
			leftTypeName,
			rightTypeName,
			compareExpression,
			"< 0"
		);
		EmitRelationalOperator(
			sb,
			indent,
			declaringType,
			ValueObjectSymbolInspector.GreaterThanOperatorName,
			">",
			leftTypeName,
			rightTypeName,
			compareExpression,
			"> 0"
		);
		EmitRelationalOperator(
			sb,
			indent,
			declaringType,
			ValueObjectSymbolInspector.LessThanOrEqualOperatorName,
			"<=",
			leftTypeName,
			rightTypeName,
			compareExpression,
			"<= 0"
		);
		EmitRelationalOperator(
			sb,
			indent,
			declaringType,
			ValueObjectSymbolInspector.GreaterThanOrEqualOperatorName,
			">=",
			leftTypeName,
			rightTypeName,
			compareExpression,
			">= 0"
		);
	}

	static void EmitRelationalOperator(
		StringBuilder sb,
		string indent,
		INamedTypeSymbol declaringType,
		string operatorMethodName,
		string operatorToken,
		string leftTypeName,
		string rightTypeName,
		string compareExpression,
		string comparisonSuffix
	)
	{
		if (
			ValueObjectSymbolInspector.HasRelationalOperator(
				declaringType,
				operatorMethodName,
				leftTypeName,
				rightTypeName
			)
		)
			return;

		sb.AppendLine(
			$@"{indent}	public static bool operator {operatorToken}({leftTypeName} left, {rightTypeName} right)
{indent}	{{
{indent}		return left.{compareExpression} {comparisonSuffix};
{indent}	}}"
		);
	}
}
