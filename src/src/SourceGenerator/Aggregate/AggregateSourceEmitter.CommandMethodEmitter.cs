using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static partial class CommandMethodEmitter
{
	public static void Generate(
		AggregateGenerationOutputContext outputContext,
		AggregateEventMethodInfo method
	)
	{
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
		var generatedReturnType =
			method.ReturnKind == EventMethodReturnKind.Aggregate
				? new TypeReferenceOptions(
					new TypeValueObject(outputContext.Aggregate.AggregateClass.TypeName, null)
				)
				: method.ReturnType;

		outputContext.Writer.WriteMethod(
			new(method.MethodName, generatedReturnType, methodAccessModifier)
			{
				IsPartial = true,
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
				var methodOutputContext = outputContext.WithWriter(body);

				if (method.Parameters.Count > 0)
					EmitParameterPreparationBlock(methodOutputContext, method);

				if (computedParameters.Count > 0)
					EmitOnComputingBefore(
						methodOutputContext,
						method,
						computedParameters,
						hookSuffix
					);

				EmitEventCreationAndShouldApply(
					methodOutputContext,
					method,
					storedParameters,
					hookSuffix,
					declareVariable: true
				);

				EmitRaisingHook(
					methodOutputContext,
					method,
					computedParameters,
					nonComputedParameters,
					hookSuffix
				);

				if (computedParameters.Count > 0)
					EmitOnComputingAfter(
						methodOutputContext,
						method,
						computedParameters,
						hookSuffix
					);

				EmitEventCreationAndShouldApply(
					methodOutputContext,
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
						EmitUnchangedCheck(methodOutputContext, method, mappedParameters);
				}

				EmitFinalization(methodOutputContext, method, hookSuffix);
			}
		);

		EmitHookDeclarations(
			outputContext,
			method,
			computedParameters,
			nonComputedParameters,
			hookSuffix
		);
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
					ifBody.WriteThrow(
						$"new global::System.ArgumentException(\"Computed parameter '{prop.ParameterName}' cannot be set by callers.\", nameof({prop.ParameterName}))"
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
							$"new global::System.ArgumentException(\"Parameter '{prop.ParameterName}' cannot be null or empty.\", nameof({prop.ParameterName}))"
						)
				);
			}
			else if (prop.IsRequired || prop.IsNotNull)
			{
				writer.WriteIfBlock(
					$"({prop.ParameterName} is null)",
					ifBody =>
						ifBody.WriteThrow(
							$"new global::System.ArgumentNullException(nameof({prop.ParameterName}))"
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

		outputContext.Writer.WriteIfBlock(
			$"!ShouldApply{hookSuffix}(@event)",
			ifBody =>
				AggregateSourceEmitter.EmitNoChangeReturn(
					outputContext.WithWriter(ifBody),
					method.ReturnKind
				)
		);
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
			new("ShouldApply" + hookSuffix, PurviewTypeLibrary.System.Boolean)
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
					new("shouldApply", PurviewTypeLibrary.System.Boolean)
					{
						Modifier = ParameterModifier.Ref,
					},
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
