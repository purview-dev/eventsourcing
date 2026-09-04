namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static class ValueObjectEmitterHelpers
{
	public static void EmitBinaryOperator(
		CodeWriter writer,
		TypeReference leftType,
		TypeReference rightType,
		string operatorToken,
		string expression
	)
	{
		writer.Operator(
			new OperatorDeclarationOptions(
				operatorToken,
				PurviewTypeLibrary.System.Boolean,
				new("left", leftType),
				new("right", rightType)
			)
			{
				Accessibility = TypeDeclarationAccessibility.Public,
			},
			body => body.Return(expression)
		);
	}

	public static void EmitRelationalOperators(
		CodeWriter writer,
		EquatableArray<string> existingOperators,
		TypeReference leftType,
		TypeReference rightType,
		string compareExpression
	)
	{
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.LessThanOperatorName,
			"<",
			leftType,
			rightType,
			compareExpression,
			"< 0"
		);
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.GreaterThanOperatorName,
			">",
			leftType,
			rightType,
			compareExpression,
			"> 0"
		);
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.LessThanOrEqualOperatorName,
			"<=",
			leftType,
			rightType,
			compareExpression,
			"<= 0"
		);
		EmitRelationalOperator(
			writer,
			existingOperators,
			ValueObjectSymbolInspector.GreaterThanOrEqualOperatorName,
			">=",
			leftType,
			rightType,
			compareExpression,
			">= 0"
		);
	}

	static void EmitRelationalOperator(
		CodeWriter writer,
		EquatableArray<string> existingOperators,
		string operatorMethodName,
		string operatorToken,
		TypeReference leftType,
		TypeReference rightType,
		string compareExpression,
		string comparisonSuffix
	)
	{
		if (existingOperators.Contains(operatorMethodName))
			return;

		writer.Operator(
			new OperatorDeclarationOptions(
				operatorToken,
				PurviewTypeLibrary.System.Boolean,
				new("left", leftType),
				new("right", rightType)
			)
			{
				Accessibility = TypeDeclarationAccessibility.Public,
			},
			body => body.Return($"left.{compareExpression} {comparisonSuffix}")
		);
	}
}
