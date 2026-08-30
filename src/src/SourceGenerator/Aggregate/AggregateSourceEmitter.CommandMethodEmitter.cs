namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static partial class CommandMethodEmitter
{
	public static void Generate(AggregateEmitContext outputContext, AggregateEventMethodInfo method)
	{
		var hookSuffix = AggregateSourceEmitter.GetHookName(method.EventType);
		var writer = outputContext.Writer;

		writer.WriteMethod(
			new(method.MethodName, method.ReturnType)
			{
				Accessibility = method.MethodAccessibility.ToTypeDeclarationAccessibility(),
				IsPartial = true,
				Parameters =
				[
					.. method.AllParameters.Select(static p => new ParameterDeclarationOptions(
						p.ParameterName,
						p.ParameterType
					)),
				],
			},
			writeBody =>
			{
				var bodyContext = outputContext.WithWriter(writeBody);

				if (!method.AllParameters.IsEmpty)
					EmitParameterPreparationBlock(bodyContext, method);

				if (!method.ComputedParameters.IsEmpty)
					EmitOnComputingBefore(bodyContext, method, hookSuffix);

				EmitEventCreationAndShouldApply(bodyContext, method, hookSuffix, declareVariable: true);

				EmitRaisingHook(bodyContext, method, hookSuffix);

				if (!method.ComputedParameters.IsEmpty)
					EmitOnComputingAfter(bodyContext, method, hookSuffix);

				EmitEventCreationAndShouldApply(bodyContext, method, hookSuffix, declareVariable: false);

				if (!method.AllParameters.IsEmpty)
				{
					var mappedParameters = method
						.AllParameters.Where(static parameter => parameter.HasAggregateProperty)
						.ToList();

					if (mappedParameters.Count > 0)
						EmitUnchangedCheck(bodyContext, method, mappedParameters);
				}

				EmitFinalization(bodyContext, method, hookSuffix);
			}
		);

		EmitHookDeclarations(outputContext, method, hookSuffix);
	}

	static void EmitParameterPreparationBlock(AggregateEmitContext outputContext, AggregateEventMethodInfo method)
	{
		var writer = outputContext.Writer;
		foreach (var prop in method.AllParameters)
		{
			if (prop.IsComputed)
			{
				writer.WriteAssignment("var", AggregateSourceEmitter.GetLocalValueName(prop), prop.ParameterName);
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
			method.AllParameters.Any(static p =>
				p.IsComputed || p.ParameterConversionKind is not EventParameterConversionKind.None
			)
		)
			writer.NewLine();

		var computedParameters = method.AllParameters.Where(static parameter => parameter.IsComputed).ToList();

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

		foreach (var prop in method.AllParameters)
		{
			if (!prop.HasAggregateProperty)
				continue;

			writer
				.Comment($"Invoke On{prop.AggregatePropertyName}Changing hook for parameter '{prop.ParameterName}'")
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

	static void EmitValidationGuards(AggregateEmitContext outputContext, AggregateEventMethodInfo method)
	{
		var writer = outputContext.Writer;
		foreach (var prop in method.AllParameters)
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
					$"{prop.ParameterName} is null",
					ifBody =>
						ifBody.WriteThrow($"new global::System.ArgumentNullException(nameof({prop.ParameterName}))")
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
		AggregateEmitContext outputContext,
		AggregateEventMethodInfo method,
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;

		writer.WriteMethodCall(
			$"OnComputing{hookSuffix}",
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.ComputedParameters)
		);

		if (!method.NonComputedParameters.IsEmpty)
		{
			writer.WriteMethodCall(
				$"OnComputing{hookSuffix}",
				AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.NonComputedParameters)
			);
		}

		writer.WriteMethodCall(
			$"OnComputing{hookSuffix}",
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.AllParameters)
		);
	}

	static void EmitEventCreationAndShouldApply(
		AggregateEmitContext outputContext,
		AggregateEventMethodInfo method,
		string hookSuffix,
		bool declareVariable
	)
	{
		AggregateSourceEmitter.EmitEventCreation(outputContext, method, declareVariable);

		outputContext.Writer.WriteIfBlock(
			$"!ShouldApply{hookSuffix}(@event)",
			ifBody => AggregateSourceEmitter.EmitNoChangeReturn(outputContext.WithWriter(ifBody), method.ReturnKind)
		);
	}

	static void EmitRaisingHook(AggregateEmitContext outputContext, AggregateEventMethodInfo method, string hookSuffix)
	{
		var writer = outputContext.Writer;
		var onRaisingMethodName = $"OnRaising{hookSuffix}";
		if (method.AllParameters.IsEmpty)
			writer.WriteMethodCall(onRaisingMethodName);
		else if (!method.ComputedParameters.IsEmpty)
		{
			if (method.NonComputedParameters.IsEmpty)
				writer.WriteMethodCall(onRaisingMethodName);
			else
			{
				writer.WriteMethodCall(
					onRaisingMethodName,
					AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.NonComputedParameters)
				);
			}

			writer.WriteMethodCall(
				onRaisingMethodName,
				AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.AllParameters)
			);
		}
		else
		{
			writer.WriteMethodCall(
				onRaisingMethodName,
				AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.AllParameters)
			);
		}
	}

	static void EmitOnComputingAfter(
		AggregateEmitContext outputContext,
		AggregateEventMethodInfo method,
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;
		var onComputingMethodName = $"OnComputing{hookSuffix}";

		writer.WriteMethodCall(
			onComputingMethodName,
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.ComputedParameters)
		);

		if (!method.NonComputedParameters.IsEmpty)
		{
			writer.WriteMethodCall(
				onComputingMethodName,
				AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.NonComputedParameters)
			);
		}

		writer.WriteMethodCall(
			onComputingMethodName,
			AggregateSourceEmitter.BuildOnCreatingCallArgumentList(method.AllParameters)
		);
	}

	static void EmitUnchangedCheck(
		AggregateEmitContext outputContext,
		AggregateEventMethodInfo method,
		List<EventPropertyInfo> mappedParameters
	)
	{
		var writer = outputContext.Writer;
		writer.WriteIfBlock(
			AggregateSourceEmitter.BuildUnchangedCondition(mappedParameters),
			ifBody => AggregateSourceEmitter.EmitNoChangeReturn(outputContext.WithWriter(ifBody), method.ReturnKind)
		);
	}

	static void EmitFinalization(AggregateEmitContext outputContext, AggregateEventMethodInfo method, string hookSuffix)
	{
		var writer = outputContext.Writer;

		writer.WriteMethodCall($"OnRaised{hookSuffix}", ["@event"]);
		writer.WriteMethodCall($"RecordAndApply", ["@event"]);

		AggregateSourceEmitter.EmitSuccessReturn(outputContext, method.ReturnKind);
	}

	static void EmitHookDeclarations(
		AggregateEmitContext outputContext,
		AggregateEventMethodInfo method,
		string hookSuffix
	)
	{
		var writer = outputContext.Writer;
		var suppression = AggregateSourceEmitter.CreateCA1822Suppression();

		if (!method.ComputedParameters.IsEmpty)
		{
			var computingMethodName = $"OnComputing{hookSuffix}";
			writer.WritePartialMethod(
				new(computingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
						method.ComputedParameters
					),
				}
			);

			if (!method.NonComputedParameters.IsEmpty)
			{
				writer.WritePartialMethod(
					new(computingMethodName)
					{
						Attributes = [suppression],
						Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
							method.NonComputedParameters
						),
					}
				);
			}

			writer.WritePartialMethod(
				new(computingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(method.AllParameters),
				}
			);
		}

		var raisingMethodName = $"OnRaising{hookSuffix}";
		if (method.AllParameters.IsEmpty)
		{
			writer.WritePartialMethod(new(raisingMethodName) { Attributes = [suppression] });
		}
		else if (!method.ComputedParameters.IsEmpty)
		{
			if (method.NonComputedParameters.IsEmpty)
				writer.WritePartialMethod(new(raisingMethodName) { Attributes = [suppression] });
			else
			{
				writer.WritePartialMethod(
					new(raisingMethodName)
					{
						Attributes = [suppression],
						Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(
							method.NonComputedParameters
						),
					}
				);
			}

			writer.WritePartialMethod(
				new(raisingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(method.AllParameters),
				}
			);
		}
		else
		{
			writer.WritePartialMethod(
				new(raisingMethodName)
				{
					Attributes = [suppression],
					Parameters = AggregateSourceEmitter.BuildOnCreatingDeclarationParameterList(method.AllParameters),
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
				writeBody.WriteMethodCall("OnShouldApply" + hookSuffix, ["@event", "ref shouldApply"]);
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
					new("shouldApply", PurviewTypeLibrary.System.Boolean) { Modifier = ParameterModifier.Ref },
				],
			}
		);

		writer.WritePartialMethod(
			new("OnRaised" + hookSuffix) { Attributes = [suppression], Parameters = [new("@event", method.EventType)] }
		);

		writer.WritePartialMethod(
			new("OnApplied" + hookSuffix) { Attributes = [suppression], Parameters = [new("@event", method.EventType)] }
		);
	}
}
