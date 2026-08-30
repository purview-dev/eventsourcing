namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static class ValueObjectEmitterHelpers
{
	public static void WriteExpressionMethod(CodeWriter writer, MethodDeclarationOptions declaration)
	{
		using (writer.WriteMethodScope(declaration))
		{
			//
		}
	}

	public static void EmitBinaryOperator(
		CodeWriter writer,
		string leftTypeName,
		string rightTypeName,
		string operatorToken,
		string expression
	)
	{
		using (
			writer.OpenBlockScope(
				$"public static bool operator {operatorToken}({leftTypeName} left, {rightTypeName} right)"
			)
		)
		{
			writer.WriteReturn(expression);
		}
	}

	public static void EmitRelationalOperators(
		CodeWriter writer,
		EquatableArray<string> existingOperators,
		string leftTypeName,
		string rightTypeName,
		string compareExpression
	)
	{
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.LessThanOperatorName,
			"<",
			leftTypeName,
			rightTypeName,
			compareExpression,
			"< 0"
		);
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.GreaterThanOperatorName,
			">",
			leftTypeName,
			rightTypeName,
			compareExpression,
			"> 0"
		);
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.LessThanOrEqualOperatorName,
			"<=",
			leftTypeName,
			rightTypeName,
			compareExpression,
			"<= 0"
		);
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.GreaterThanOrEqualOperatorName,
			">=",
			leftTypeName,
			rightTypeName,
			compareExpression,
			">= 0"
		);
	}

	static void EmitRelationalOperator(
		CodeWriter writer,
		EquatableArray<string> existingOperators,
		string operatorMethodName,
		string operatorToken,
		string leftTypeName,
		string rightTypeName,
		string compareExpression,
		string comparisonSuffix
	)
	{
		if (existingOperators.Contains(operatorMethodName))
			return;

		using (
			writer.OpenBlockScope(
				$"public static bool operator {operatorToken}({leftTypeName} left, {rightTypeName} right)"
			)
		)
		{
			writer.WriteReturn($"left.{compareExpression} {comparisonSuffix}");
		}
	}
}
