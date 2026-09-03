using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Common;

/// <summary>
/// Structural helpers for assertions the framework's <c>CodeQuery</c> does not model directly,
/// such as operator declarations (which are <c>OperatorDeclarationSyntax</c>, not methods).
/// </summary>
static class SyntaxAssertionHelpers
{
	public static bool HasOperator(
		this TypeDeclarationSyntax type,
		string operatorToken,
		params string[] parameterTypeContains
	) => GetOperator(type, operatorToken, parameterTypeContains) is not null;

	public static OperatorDeclarationSyntax? GetOperator(
		this TypeDeclarationSyntax type,
		string operatorToken,
		params string[] parameterTypeContains
	)
	{
		foreach (var candidate in type.Members.OfType<OperatorDeclarationSyntax>())
		{
			if (candidate.OperatorToken.Text != operatorToken)
				continue;

			var parameters = candidate.ParameterList.Parameters;
			if (parameters.Count != parameterTypeContains.Length)
				continue;

			var allMatch = true;
			for (var index = 0; index < parameters.Count; index++)
			{
				if (
					parameters[index].Type?.ToString().Contains(parameterTypeContains[index], StringComparison.Ordinal)
					!= true
				)
				{
					allMatch = false;
					break;
				}
			}

			if (allMatch)
				return candidate;
		}

		return null;
	}
}
