namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static partial class AggregateSourceEmitter
{
	static class CollectionCommandMethodEmitter
	{
		public static void Generate(CodeWriter writer, AggregateEventMethodInfo method)
		{
			var collectionEvent = method.CollectionEvent!;
			var parameter = method.AllParameters[0];
			var hookSuffix = GetHookName(method.EventType);
			var normalizeValidateSuffix = collectionEvent.NormalizeValidateHookSuffix;
			var methodAccessModifier = GetAccessModifierString(method.MethodAccessibility);
			var parameterList = BuildParameterList(method.AllParameters);

			writer.WriteLine(
				$"{methodAccessModifier} partial {method.ReturnType} {method.MethodName}({parameterList})"
			);

			using (writer.OpenBlockScope())
			{
				EmitCollectionGuard(writer, collectionEvent);

				if (collectionEvent.ParameterShape == CollectionParameterShape.Single)
					EmitSingleItemBody(writer, method, collectionEvent, parameter, hookSuffix, normalizeValidateSuffix);
				else
					EmitEnumerableBody(writer, method, collectionEvent, parameter, hookSuffix, normalizeValidateSuffix);

				EmitCollectionFinalization(writer, method, hookSuffix);
			}

			writer.NewLine();

			EmitCollectionTrailingHookDeclarations(writer, method, collectionEvent, parameter, hookSuffix);
		}

		static void EmitCollectionGuard(CodeWriter writer, CollectionEventInfo collectionEvent)
		{
			writer.WriteBlock(
				$"if ({collectionEvent.PropertyName} is null)",
				block =>
					block.WriteThrow(
						$"new global::System.InvalidOperationException(\"Collection property '{collectionEvent.PropertyName}' cannot be null.\")"
					)
			);
			writer.NewLine();
		}

		static void EmitSingleItemBody(
			CodeWriter writer,
			AggregateEventMethodInfo method,
			CollectionEventInfo collectionEvent,
			EventPropertyInfo parameter,
			string hookSuffix,
			string normalizeValidateSuffix
		)
		{
			writer.WriteAssignment("var", "__itemValue", parameter.ParameterName);
			writer.WriteMethodCall(
				$"OnNormalizing{normalizeValidateSuffix}",
				[new MethodCallArgumentOptions("__itemValue", ParameterModifier.Ref)]
			);
			writer.WriteMethodCall(
				$"OnValidating{normalizeValidateSuffix}",
				[new MethodCallArgumentOptions("__itemValue", ParameterModifier.Ref)]
			);

			var isAddOperation = collectionEvent.Operation == CollectionMutationOperation.Add;
			if (isAddOperation && collectionEvent.IsSet)
			{
				writer.WriteIfBlock(
					$"{collectionEvent.PropertyName}.Contains(__itemValue)",
					ifBody => EmitNoChangeReturn(ifBody, method.ReturnKind)
				);
			}
			else if (!isAddOperation)
			{
				writer.WriteIfBlock(
					$"!{collectionEvent.PropertyName}.Contains(__itemValue)",
					ifBody => EmitNoChangeReturn(ifBody, method.ReturnKind)
				);
			}

			writer.Write("var @event = new ").Write(method.EventType);
			using (writer.OpenBlockScope())
			{
				writer.WriteLine($"{parameter.PropertyName} = __itemValue,");
			}
			writer.Write(";").NewLine();

			writer.WriteIfBlock(
				$"!ShouldApply{hookSuffix}(@event)",
				ifBody => EmitNoChangeReturn(ifBody, method.ReturnKind)
			);

			writer.WriteMethodCall(
				$"OnRaising{hookSuffix}",
				[new MethodCallArgumentOptions("__itemValue", ParameterModifier.Ref)]
			);
		}

		static void EmitEnumerableBody(
			CodeWriter writer,
			AggregateEventMethodInfo method,
			CollectionEventInfo collectionEvent,
			EventPropertyInfo parameter,
			string hookSuffix,
			string normalizeValidateSuffix
		)
		{
			var enumerableType = TypeLibrary.System.Collections.Generic.IEnumerable.MakeGeneric(
				collectionEvent.ElementType
			);

			writer.WriteAssignment(
				$"global::System.Collections.Generic.IEnumerable<{collectionEvent.ElementType}>",
				"__itemsValue",
				parameter.ParameterName
			);
			writer.WriteMethodCall(
				$"OnNormalizing{normalizeValidateSuffix}",
				[new MethodCallArgumentOptions("__itemsValue", ParameterModifier.Ref)]
			);
			writer.WriteMethodCall(
				$"OnValidating{normalizeValidateSuffix}",
				[new MethodCallArgumentOptions("__itemsValue", ParameterModifier.Ref)]
			);
			writer.WriteAssignment(
				"var",
				"__eventItems",
				$"__itemsValue as {collectionEvent.ElementType}[] ?? [.. __itemsValue]"
			);
			writer.WriteIfBlock("__eventItems.Length == 0", ifBody => EmitNoChangeReturn(ifBody, method.ReturnKind));

			var isAddOperation = collectionEvent.Operation == CollectionMutationOperation.Add;
			if (isAddOperation && collectionEvent.IsSet)
			{
				writer.WriteAssignment("var", "__hasNewValues", "false");
				writer.WriteBlock(
					"foreach (var __item in __eventItems)",
					block =>
						block.WriteIfBlock(
							$"!{collectionEvent.PropertyName}.Contains(__item)",
							ifBody =>
							{
								ifBody.WriteAssignment("__hasNewValues", "true");
								ifBody.WriteLine("break;");
							}
						)
				);
				writer.WriteIfBlock("!__hasNewValues", ifBody => EmitNoChangeReturn(ifBody, method.ReturnKind));
			}
			else if (!isAddOperation)
			{
				writer.WriteAssignment("var", "__hasExistingValues", "false");
				writer.WriteBlock(
					"foreach (var __item in __eventItems)",
					block =>
						block.WriteIfBlock(
							$"{collectionEvent.PropertyName}.Contains(__item)",
							ifBody =>
							{
								ifBody.WriteAssignment("__hasExistingValues", "true");
								ifBody.WriteLine("break;");
							}
						)
				);
				writer.WriteIfBlock("!__hasExistingValues", ifBody => EmitNoChangeReturn(ifBody, method.ReturnKind));
			}

			writer.Write("var @event = new ").Write(method.EventType);
			using (writer.OpenBlockScope())
			{
				writer.WriteLine($"{parameter.PropertyName} = __eventItems,");
			}
			writer.Write(";").NewLine();

			writer.WriteIfBlock(
				$"!ShouldApply{hookSuffix}(@event)",
				ifBody => EmitNoChangeReturn(ifBody, method.ReturnKind)
			);

			writer.WriteMethodCall(
				$"OnRaising{hookSuffix}",
				[new MethodCallArgumentOptions("__itemsValue", ParameterModifier.Ref)]
			);
		}

		static void EmitCollectionFinalization(CodeWriter writer, AggregateEventMethodInfo method, string hookSuffix)
		{
			writer.NewLine();
			writer.WriteMethodCall($"OnRaised{hookSuffix}", ["@event"]);
			writer.WriteMethodCall("RecordAndApply", ["@event"]);
			EmitSuccessReturn(writer, method.ReturnKind);
		}

		static void EmitCollectionTrailingHookDeclarations(
			CodeWriter writer,
			AggregateEventMethodInfo method,
			CollectionEventInfo collectionEvent,
			EventPropertyInfo parameter,
			string hookSuffix
		)
		{
			var suppression = CreateCA1822Suppression();

			var normalizeValidateType =
				collectionEvent.ParameterShape == CollectionParameterShape.Single
					? collectionEvent.ElementType
					: TypeLibrary
						.System.Collections.Generic.IEnumerable.MakeGeneric(collectionEvent.ElementType)
						.AsTypeReference();
			var normalizingParam = new ParameterDeclarationOptions(parameter.ParameterName, normalizeValidateType)
			{
				Modifier = ParameterModifier.Ref,
			};

			writer.WritePartialMethod(
				new($"OnRaising{hookSuffix}") { Attributes = [suppression], Parameters = [normalizingParam] }
			);

			writer.WritePartialMethod(
				new($"OnRaised{hookSuffix}")
				{
					Attributes = [suppression],
					Parameters = [new("@event", method.EventType)],
				}
			);

			writer.WriteMethod(
				new($"ShouldApply{hookSuffix}", PurviewTypeLibrary.System.Boolean)
				{
					Parameters = [new("@event", method.EventType)],
				},
				writeBody =>
				{
					writeBody.WriteAssignment("var", "shouldApply", "true");
					writeBody.WriteMethodCall($"OnShouldApply{hookSuffix}", ["@event", "ref shouldApply"]);
					writeBody.WriteReturn("shouldApply");
				}
			);

			writer.WritePartialMethod(
				new($"OnShouldApply{hookSuffix}")
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
				new($"OnApplied{hookSuffix}")
				{
					Attributes = [suppression],
					Parameters = [new("@event", method.EventType)],
				}
			);

			writer.NewLine();
		}
	}
}
