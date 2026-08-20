using System.Text;
using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static partial class CommandMethodEmitter
{
	public static void Generate(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method
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
		var hookSuffix = AggregateSourceEmitter.GetHookName(method.EventType);
		var methodAccessModifier = AggregateSourceEmitter.GetAccessModifier(
			method.MethodAccessibility
		);

		outputContext.Writer.WriteMethod(
			new(method.MethodName, method.ReturnType, methodAccessModifier)
			{
				Parameters =
				[
					.. method.Parameters.Select(p => new ParameterDeclarationOptions(
						p.ParameterName,
						p.ParameterType
					)
					{
						IsParams = p.IsParams,
					}),
				],
			},
			body =>
			{
				if (method.Parameters.Count > 0)
					EmitParameterPreparationBlock(outputContext, method);

				if (computedParameters.Count > 0)
					EmitOnComputingBefore(outputContext, method, computedParameters, hookSuffix);

				EmitEventCreationAndShouldApply(
					outputContext,
					method,
					storedParameters,
					hookSuffix,
					declareVariable: true
				);

				EmitRaisingHook(
					outputContext,
					method,
					computedParameters,
					nonComputedParameters,
					hookSuffix
				);

				if (computedParameters.Count > 0)
					EmitOnComputingAfter(outputContext, method, computedParameters, hookSuffix);

				EmitEventCreationAndShouldApply(
					outputContext,
					method,
					storedParameters,
					hookSuffix,
					declareVariable: false
				);

				if (method.Parameters.Count > 0)
				{
					var mappedParameters = method
						.Parameters.Where(static parameter => parameter.HasAggregateProperty)
						.ToList();

					if (mappedParameters.Count > 0)
						EmitUnchangedCheck(outputContext, method, mappedParameters);
				}

				EmitFinalization(outputContext, method, hookSuffix);

				EmitHookDeclarations(
					outputContext,
					method,
					computedParameters,
					nonComputedParameters,
					hookSuffix
				);
			}
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
				$"{paramsPrefix}{method.Parameters[i].ParameterType} {method.Parameters[i].ParameterName}"
			);
		}

		return paramList.ToString();
	}

	static void EmitParameterPreparationBlock(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method
	)
	{
		var writer = outputContext.Writer;
		foreach (var prop in method.Parameters)
		{
			if (prop.IsComputed)
			{
				writer.WriteAssignment(
					"var",
					AggregateSourceEmitter.GetLocalValueName(prop),
					prop.ParameterName
				);
				continue;
			}

			if (prop.ParameterConversionKind is not EventParameterConversionKind.None)
			{
				writer.WriteAssignment(
					"var",
					AggregateSourceEmitter.GetLocalValueName(prop),
					AggregateSourceEmitter.BuildPropertyValueExpression(outputContext, method, prop)
				);
				continue;
			}
		}

		if (
			method.Parameters.Any(static p =>
				p.IsComputed || p.ParameterConversionKind is not EventParameterConversionKind.None
			)
		)
			writer.NewLine();

		var computedParameters = method
			.Parameters.Where(static parameter => parameter.IsComputed)
			.ToList();

		foreach (var prop in computedParameters)
		{
			var ifCondition =
				$"!global::System.Collections.Generic.EqualityComparer<{prop.PropertyType}>.Default.Equals({prop.ParameterName}, default({prop.PropertyType}))";

			writer.WriteIfBlock(
				ifCondition,
				ifBody =>
					ifBody.WriteLine(
						$"throw new global::System.ArgumentException(\"Computed parameter '{prop.ParameterName}' cannot be set by callers.\", nameof({prop.ParameterName}));"
					)
			);
		}

		EmitValidationGuards(outputContext, method);

		foreach (var prop in method.Parameters)
		{
			if (!prop.HasAggregateProperty)
				continue;

			writer
				.Comment(
					$"Invoke On{prop.AggregatePropertyName}Changing hook for parameter '{prop.ParameterName}'"
				)
				.WriteMethodCall(
					$"On{prop.AggregatePropertyName}Changing",
					[
						new MethodCallArgumentOptions(
							AggregateSourceEmitter.GetWorkingValueName(prop),
							ParameterModifier.Ref
						),
					]
				);
		}
	}

	static void EmitValidationGuards(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method
	)
	{
		var writer = outputContext.Writer;
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
				writer.WriteIfBlock(
					$"global::System.String.IsNullOrWhiteSpace({prop.ParameterName})",
					ifBody =>
						ifBody.WriteThrow(
							$"global::System.ArgumentException(\"Parameter '{prop.ParameterName}' cannot be null or empty.\", nameof({prop.ParameterName}));"
						)
				);
			}
			else if (prop.IsRequired || prop.IsNotNull)
			{
				writer.WriteIfBlock(
					$"({prop.ParameterName} is null)",
					ifBody =>
						ifBody.WriteThrow(
							$"global::System.ArgumentNullException(nameof({prop.ParameterName}));"
						)
				);
			}

			writer.WriteAssignment(
				"var",
				AggregateSourceEmitter.GetLocalValueName(prop),
				prop.ParameterName,
				forceNotNull: true
			);
		}
	}

	static void EmitOnComputingBefore(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> computedParameters,
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;

		writer.WriteMethodCall(
			$"OnComputing{hookSuffix}",
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(computedParameters)
		);
		writer.WriteMethodCall(
			$"OnComputing{hookSuffix}",
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)
		);
	}

	static void EmitEventCreationAndShouldApply(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> storedParameters,
		string hookSuffix,
		bool declareVariable
	)
	{
		AggregateSourceEmitter.EmitEventCreation(
			outputContext,
			method,
			storedParameters,
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
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;
		var onRaisingMethodName = $"OnRaising{hookSuffix}";
		if (method.Parameters.Count == 0)
			writer.WriteMethodCall(onRaisingMethodName);
		else if (computedParameters.Count > 0)
		{
			if (nonComputedParameters.Count == 0)
				writer.WriteMethodCall(onRaisingMethodName);
			else
			{
				writer.WriteMethodCall(
					onRaisingMethodName,
					AggregateSourceEmitter.BuildOnCreatingCallArgumentList(nonComputedParameters)
				);
			}

			writer.WriteMethodCall(
				onRaisingMethodName,
				AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)
			);
		}
		else
		{
			writer.WriteMethodCall(
				onRaisingMethodName,
				AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)
			);
		}
	}

	static void EmitOnComputingAfter(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> computedParameters,
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;
		var onComputingMethodName = $"OnComputing{hookSuffix}";

		writer.WriteMethodCall(
			onComputingMethodName,
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(computedParameters)
		);
		writer.WriteMethodCall(
			onComputingMethodName,
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.Parameters)
		);
	}

	static void EmitUnchangedCheck(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> mappedParameters
	)
	{
		var writer = outputContext.Writer;
		writer.WriteIfBlock(
			AggregateSourceEmitter.BuildUnchangedCondition(mappedParameters),
			ifBody =>
				AggregateSourceEmitter.EmitNoChangeReturn(
					outputContext.WithWriter(ifBody),
					method.ReturnKind
				)
		);
	}

	static void EmitFinalization(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;

		writer.WriteMethodCall($"OnRaised{hookSuffix}", ["@event"]);
		writer.WriteMethodCall($"RecordAndApply", ["@event"]);

		AggregateSourceEmitter.EmitSuccessReturn(outputContext, method.ReturnKind);
	}

	static void EmitHookDeclarations(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> computedParameters,
		List<EventPropertyInfo> nonComputedParameters,
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;
		var suppression = AggregateSourceEmitter.CreateCA1822Suppression();

		if (computedParameters.Count > 0)
		{
			var computingMethodName = $"OnComputing{hookSuffix}";
			writer.WritePartialMethod(
				new(computingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
						computedParameters
					),
				}
			);

			writer.WritePartialMethod(
				new(computingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
						method.Parameters
					),
				}
			);
		}

		var raisingMethodName = $"OnRaising{hookSuffix}";
		if (method.Parameters.Count == 0)
		{
			writer.WritePartialMethod(new(raisingMethodName) { Attributes = [suppression] });
		}
		else if (computedParameters.Count > 0)
		{
			if (nonComputedParameters.Count == 0)
				writer.WritePartialMethod(new(raisingMethodName) { Attributes = [suppression] });
			else
			{
				writer.WritePartialMethod(
					new(raisingMethodName)
					{
						Attributes = [suppression],
						Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
							nonComputedParameters
						),
					}
				);
			}

			writer.WritePartialMethod(
				new(raisingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
						method.Parameters
					),
				}
			);
		}
		else
		{
			writer.WritePartialMethod(
				new(raisingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
						method.Parameters
					),
				}
			);
		}

		writer.WriteMethod(
			new("ShouldApply", PurviewTypeLibrary.System.Boolean)
			{
				Parameters = [new ParameterDeclarationOptions("@event", method.EventType)],
			},
			writeBody =>
			{
				writeBody.WriteAssignment("var", "shouldApply", "true");
				writeBody.WriteMethodCall(
					"OnShouldApply" + hookSuffix,
					["@event", "ref shouldApply"]
				);
				writeBody.WriteReturn("shouldApply");
			}
		);

		writer.WritePartialMethod(
			new("OnShouldApply" + hookSuffix)
			{
				Attributes = [suppression],
				Parameters =
				[
					new("@event", method.EventType),
					new("shouldApply", PurviewTypeLibrary.System.Boolean, ParameterModifier.Ref),
				],
			}
		);

		writer.WritePartialMethod(
			new("OnRaised" + hookSuffix)
			{
				Attributes = [suppression],
				Parameters = [new("@event", method.EventType)],
			}
		);

		writer.WritePartialMethod(
			new("OnApplied" + hookSuffix)
			{
				Attributes = [suppression],
				Parameters = [new("@event", method.EventType)],
			}
		);
	}
}
