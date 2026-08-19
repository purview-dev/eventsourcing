using System.Text;
using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static partial class CommandMethodEmitter
{
	public static void Generate(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		string indent
	)
	{
		var paramList = BuildParameterList(method);
		var computedParameters = method
			.Parameters.Where(static parameter => parameter.IsComputed)
			.ToList();
		var nonComputedParameters = method
			.Parameters.Where(static parameter => !parameter.IsComputed)
			.ToList();
		var storedParameters = method
			.Parameters.Where(static parameter => parameter.IncludeInEvent)
			.ToList();
		var hookSuffix = AggregateSourceEmitter.GetHookName(method.EventName);
		var methodAccessModifier = AggregateSourceEmitter.GetAccessModifier(
			method.MethodAccessibility
		);

		outputContext.Writer.WriteLine(
			$"{indent}\t{methodAccessModifier} partial {method.ReturnTypeName} {method.MethodName}({paramList})"
		);
		outputContext.Writer.WriteLine($"{indent}\t{{");

		if (method.Parameters.Count > 0)
			EmitParameterPreparationBlock(outputContext, method, indent);

		if (computedParameters.Count > 0)
			EmitOnComputingBefore(outputContext, method, computedParameters, hookSuffix, indent);

		EmitEventCreationAndShouldApply(
			outputContext,
			method,
			storedParameters,
			hookSuffix,
			indent,
			declareVariable: true
		);

		outputContext.Writer.WriteLine();
		EmitRaisingHook(
			outputContext,
			method,
			computedParameters,
			nonComputedParameters,
			hookSuffix,
			indent
		);

		if (computedParameters.Count > 0)
		{
			outputContext.Writer.WriteLine();
			EmitOnComputingAfter(outputContext, method, computedParameters, hookSuffix, indent);
		}

		EmitEventCreationAndShouldApply(
			outputContext,
			method,
			storedParameters,
			hookSuffix,
			indent,
			declareVariable: false
		);

		if (method.Parameters.Count > 0)
		{
			var mappedParameters = method
				.Parameters.Where(static parameter => parameter.HasAggregateProperty)
				.ToList();

			if (mappedParameters.Count > 0)
			{
				outputContext.Writer.WriteLine();
				EmitUnchangedCheck(outputContext, method, mappedParameters, indent);
			}
		}

		EmitFinalization(outputContext, method, hookSuffix, indent);

		outputContext.Writer.WriteLine($"{indent}\t}}");
		outputContext.Writer.WriteLine();

		EmitHookDeclarations(
			outputContext,
			method,
			computedParameters,
			nonComputedParameters,
			hookSuffix,
			indent
		);
	}

	static string BuildParameterList(AggregateEventMethodInfo method)
	{
		var paramList = new StringBuilder();
		for (var i = 0; i < method.Parameters.Count; i++)
		{
			if (i > 0)
				paramList.Append(", ");
			var paramsPrefix = method.Parameters[i].IsParams ? "params " : string.Empty;
			paramList.Append(
				$"{paramsPrefix}{method.Parameters[i].ParameterTypeName} {method.Parameters[i].ParameterName}"
			);
		}

		return paramList.ToString();
	}

	static void EmitParameterPreparationBlock(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		string indent
	)
	{
		var mappedParameters = method
			.Parameters.Where(static parameter => parameter.HasAggregateProperty)
			.ToList();

		foreach (var prop in method.Parameters)
		{
			if (prop.IsComputed)
			{
				outputContext.Writer.WriteLine(
					$"{indent}\t\tvar {AggregateSourceEmitter.GetLocalValueName(prop)} = {prop.ParameterName};"
				);
				continue;
			}

			if (prop.ParameterConversionKind is not EventParameterConversionKind.None)
			{
				outputContext.Writer.WriteLine(
					$"{indent}\t\tvar {AggregateSourceEmitter.GetLocalValueName(prop)} = {AggregateSourceEmitter.BuildPropertyValueExpression(outputContext, method, prop)};"
				);
				continue;
			}
		}

		if (
			method.Parameters.Any(static p =>
				p.IsComputed || p.ParameterConversionKind is not EventParameterConversionKind.None
			)
		)
			outputContext.Writer.WriteLine();

		var computedParameters = method
			.Parameters.Where(static parameter => parameter.IsComputed)
			.ToList();

		foreach (var prop in computedParameters)
		{
			outputContext.Writer.WriteLine(
				$"{indent}\t\tif (!global::System.Collections.Generic.EqualityComparer<{prop.PropertyTypeName}>.Default.Equals({prop.ParameterName}, default({prop.PropertyTypeName})))"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t{{");
			outputContext.Writer.WriteLine(
				$"{indent}\t\t\tthrow new global::System.ArgumentException(\"Computed parameter '{prop.ParameterName}' cannot be set by callers.\", nameof({prop.ParameterName}));"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t}}");
		}

		if (computedParameters.Count > 0)
			outputContext.Writer.WriteLine();

		EmitValidationGuards(outputContext, method, indent);

		foreach (var prop in mappedParameters)
		{
			outputContext.Writer.WriteLine(
				$"{indent}\t\tOn{prop.AggregatePropertyName}Changing(ref {AggregateSourceEmitter.GetWorkingValueName(prop)});"
			);
		}

		if (mappedParameters.Count > 0)
			outputContext.Writer.WriteLine();
	}

	static void EmitValidationGuards(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		string indent
	)
	{
		var emitted = false;
		foreach (var prop in method.Parameters)
		{
			if (prop.IsComputed)
				continue;

			if (prop.ParameterConversionKind is not EventParameterConversionKind.None)
				continue;

			if (!prop.RequiresLocalCopy)
				continue;

			if (prop.IsRequired && prop.IsString)
			{
				outputContext.Writer.WriteLine(
					$"{indent}\t\tif (global::System.String.IsNullOrWhiteSpace({prop.ParameterName}))"
				);
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				outputContext.Writer.WriteLine(
					$"{indent}\t\t\tthrow new global::System.ArgumentException(\"Parameter '{prop.ParameterName}' cannot be null or empty.\", nameof({prop.ParameterName}));"
				);
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
			}
			else if (prop.IsRequired || prop.IsNotNull)
			{
				outputContext.Writer.WriteLine($"{indent}\t\tif ({prop.ParameterName} is null)");
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				outputContext.Writer.WriteLine(
					$"{indent}\t\t\tthrow new global::System.ArgumentNullException(nameof({prop.ParameterName}));"
				);
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
			}

			outputContext.Writer.WriteLine(
				$"{indent}\t\tvar {AggregateSourceEmitter.GetLocalValueName(prop)} = {prop.ParameterName}!;"
			);

			emitted = true;
		}

		if (emitted)
			outputContext.Writer.WriteLine();
	}

	static void EmitOnComputingBefore(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> computedParameters,
		string hookSuffix,
		string indent
	)
	{
		outputContext.Writer.WriteLine(
			$"{indent}\t\tOnComputing{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingCallArgumentList(computedParameters)});"
		);
		outputContext.Writer.WriteLine(
			$"{indent}\t\tOnComputing{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)});"
		);
		outputContext.Writer.WriteLine();
	}

	static void EmitEventCreationAndShouldApply(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> storedParameters,
		string hookSuffix,
		string indent,
		bool declareVariable
	)
	{
		AggregateSourceEmitter.EmitEventCreation(
			outputContext,
			method,
			storedParameters,
			indent,
			declareVariable
		);

		outputContext.Writer.WriteLine($"{indent}\t\tif (!ShouldApply{hookSuffix}(@event))");
		outputContext.Writer.WriteLine($"{indent}\t\t{{");

		AggregateSourceEmitter.EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
		outputContext.Writer.WriteLine($"{indent}\t\t}}");
	}

	static void EmitRaisingHook(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> computedParameters,
		List<EventPropertyInfo> nonComputedParameters,
		string hookSuffix,
		string indent
	)
	{
		if (method.Parameters.Count == 0)
			outputContext.Writer.WriteLine($"{indent}\t\tOnRaising{hookSuffix}();");
		else if (computedParameters.Count > 0)
		{
			if (nonComputedParameters.Count == 0)
				outputContext.Writer.WriteLine($"{indent}\t\tOnRaising{hookSuffix}();");
			else
				outputContext.Writer.WriteLine(
					$"{indent}\t\tOnRaising{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingCallArgumentList(nonComputedParameters)});"
				);
			outputContext.Writer.WriteLine(
				$"{indent}\t\tOnRaising{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)});"
			);
		}
		else
		{
			outputContext.Writer.WriteLine(
				$"{indent}\t\tOnRaising{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)});"
			);
		}
	}

	static void EmitOnComputingAfter(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> computedParameters,
		string hookSuffix,
		string indent
	)
	{
		outputContext.Writer.WriteLine(
			$"{indent}\t\tOnComputing{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingCallArgumentList(computedParameters)});"
		);
		outputContext.Writer.WriteLine(
			$"{indent}\t\tOnComputing{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)});"
		);
	}

	static void EmitUnchangedCheck(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> mappedParameters,
		string indent
	)
	{
		outputContext.Writer.WriteLine(
			$"{indent}\t\tif ({AggregateSourceEmitter.BuildUnchangedCondition(mappedParameters)})"
		);
		outputContext.Writer.WriteLine($"{indent}\t\t{{");
		AggregateSourceEmitter.EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
		outputContext.Writer.WriteLine($"{indent}\t\t}}");
	}

	static void EmitFinalization(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		string hookSuffix,
		string indent
	)
	{
		outputContext.Writer.WriteLine($"{indent}\t\tOnRaised{hookSuffix}(@event);");
		outputContext.Writer.WriteLine($"{indent}\t\tRecordAndApply(@event);");
		outputContext.Writer.WriteLine();
		AggregateSourceEmitter.EmitSuccessReturn(outputContext, method.ReturnKind, indent, 2);
	}

	static void EmitHookDeclarations(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> computedParameters,
		List<EventPropertyInfo> nonComputedParameters,
		string hookSuffix,
		string indent
	)
	{
		if (computedParameters.Count > 0)
		{
			AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
			outputContext.Writer.WriteLine(
				$"{indent}\tpartial void OnComputing{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(computedParameters)});"
			);
			AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
			outputContext.Writer.WriteLine(
				$"{indent}\tpartial void OnComputing{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(method.Parameters)});"
			);
		}

		AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
		if (method.Parameters.Count == 0)
			outputContext.Writer.WriteLine($"{indent}\tpartial void OnRaising{hookSuffix}();");
		else if (computedParameters.Count > 0)
		{
			if (nonComputedParameters.Count == 0)
				outputContext.Writer.WriteLine($"{indent}\tpartial void OnRaising{hookSuffix}();");
			else
				outputContext.Writer.WriteLine(
					$"{indent}\tpartial void OnRaising{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(nonComputedParameters)});"
				);
			AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
			outputContext.Writer.WriteLine(
				$"{indent}\tpartial void OnRaising{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(method.Parameters)});"
			);
		}
		else
		{
			outputContext.Writer.WriteLine(
				$"{indent}\tpartial void OnRaising{hookSuffix}({AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(method.Parameters)});"
			);
		}

		AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
		outputContext.Writer.WriteLine(
			$"{indent}\tbool ShouldApply{hookSuffix}(global::{method.EventNamespace}.{method.EventName} @event)"
		);
		outputContext.Writer.WriteLine($"{indent}\t{{");
		outputContext.Writer.WriteLine($"{indent}\t\tvar shouldApply = true;");
		outputContext.Writer.WriteLine(
			$"{indent}\t\tOnShouldApply{hookSuffix}(@event, ref shouldApply);"
		);
		outputContext.Writer.WriteLine($"{indent}\t\treturn shouldApply;");
		outputContext.Writer.WriteLine($"{indent}\t}}");
		AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
		outputContext.Writer.WriteLine(
			$"{indent}\tpartial void OnShouldApply{hookSuffix}(global::{method.EventNamespace}.{method.EventName} @event, ref bool shouldApply);"
		);
		AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
		outputContext.Writer.WriteLine(
			$"{indent}\tpartial void OnRaised{hookSuffix}(global::{method.EventNamespace}.{method.EventName} @event);"
		);
		AggregateSourceEmitter.EmitCa1822Suppression(outputContext, indent);
		outputContext.Writer.WriteLine(
			$"{indent}\tpartial void OnApplied{hookSuffix}(global::{method.EventNamespace}.{method.EventName} @event);"
		);
		outputContext.Writer.WriteLine();
	}
}
