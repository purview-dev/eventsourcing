namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static partial class AggregateSourceEmitter
{
	static class CollectionCommandMethodEmitter
	{
		public static void Generate(
			AggregateGenerationOutputContext outputContext,
			AggregateEventMethodInfo method,
			string indent
		)
		{
			var collectionEvent = method.CollectionEvent!;
			var parameter = method.Parameters[0];
			var hookSuffix = GetHookName(method.EventName);
			var normalizeValidateSuffix = collectionEvent.NormalizeValidateHookSuffix;
			var methodAccessModifier = GetAccessModifier(method.MethodAccessibility);

			outputContext.Writer.WriteLine(
				$"{indent}\t{methodAccessModifier} partial {method.ReturnTypeName} {method.MethodName}({(parameter.IsParams ? "params " : string.Empty)}{parameter.ParameterTypeName} {parameter.ParameterName})"
			);
			outputContext.Writer.WriteLine($"{indent}\t{{");

			EmitCollectionGuard(outputContext, collectionEvent, indent);

			if (collectionEvent.ParameterShape == CollectionParameterShape.Single)
				EmitSingleItemBody(
					outputContext,
					method,
					collectionEvent,
					parameter,
					hookSuffix,
					normalizeValidateSuffix,
					indent
				);
			else
				EmitEnumerableBody(
					outputContext,
					method,
					collectionEvent,
					parameter,
					hookSuffix,
					normalizeValidateSuffix,
					indent
				);

			EmitCollectionFinalization(outputContext, method, hookSuffix, indent);

			outputContext.Writer.WriteLine($"{indent}\t}}");
			outputContext.Writer.WriteLine();

			EmitCollectionTrailingHookDeclarations(
				outputContext,
				method,
				collectionEvent,
				parameter,
				hookSuffix,
				indent
			);
		}

		static void EmitCollectionGuard(
			AggregateGenerationOutputContext outputContext,
			CollectionEventInfo collectionEvent,
			string indent
		)
		{
			outputContext.Writer.WriteLine(
				$"{indent}\t\tif ({collectionEvent.PropertyName} is null)"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t{{");
			outputContext.Writer.WriteLine(
				$"{indent}\t\t\tthrow new global::System.InvalidOperationException(\"Collection property '{collectionEvent.PropertyName}' cannot be null.\");"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t}}");
			outputContext.Writer.WriteLine();
		}

		static void EmitSingleItemBody(
			AggregateGenerationOutputContext outputContext,
			AggregateEventMethodInfo method,
			CollectionEventInfo collectionEvent,
			EventPropertyInfo parameter,
			string hookSuffix,
			string normalizeValidateSuffix,
			string indent
		)
		{
			outputContext.Writer.WriteLine(
				$"{indent}\t\tvar __itemValue = {parameter.ParameterName};"
			);
			outputContext.Writer.WriteLine(
				$"{indent}\t\tOnNormalizing{normalizeValidateSuffix}(ref __itemValue);"
			);
			outputContext.Writer.WriteLine(
				$"{indent}\t\tOnValidating{normalizeValidateSuffix}(__itemValue);"
			);

			var isAddOperation = collectionEvent.Operation == CollectionMutationOperation.Add;
			if (isAddOperation && collectionEvent.IsSet)
			{
				outputContext.Writer.WriteLine();
				outputContext.Writer.WriteLine(
					$"{indent}\t\tif ({collectionEvent.PropertyName}.Contains(__itemValue))"
				);
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
			}
			else if (!isAddOperation)
			{
				outputContext.Writer.WriteLine();
				outputContext.Writer.WriteLine(
					$"{indent}\t\tif (!{collectionEvent.PropertyName}.Contains(__itemValue))"
				);
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
			}

			outputContext.Writer.WriteLine();
			outputContext.Writer.WriteLine(
				$"{indent}\t\tvar @event = new global::{method.EventNamespace}.{method.EventName}"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t{{");
			outputContext.Writer.WriteLine(
				$"{indent}\t\t\t{parameter.PropertyName} = __itemValue,"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t}};");
			outputContext.Writer.WriteLine($"{indent}\t\tif (!ShouldApply{hookSuffix}(@event))");
			outputContext.Writer.WriteLine($"{indent}\t\t{{");
			EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
			outputContext.Writer.WriteLine($"{indent}\t\t}}");
			outputContext.Writer.WriteLine();
			outputContext.Writer.WriteLine($"{indent}\t\tOnRaising{hookSuffix}(ref __itemValue);");
		}

		static void EmitEnumerableBody(
			AggregateGenerationOutputContext outputContext,
			AggregateEventMethodInfo method,
			CollectionEventInfo collectionEvent,
			EventPropertyInfo parameter,
			string hookSuffix,
			string normalizeValidateSuffix,
			string indent
		)
		{
			outputContext.Writer.WriteLine(
				$"{indent}\t\tglobal::System.Collections.Generic.IEnumerable<{collectionEvent.ElementTypeName}> __itemsValue = {parameter.ParameterName};"
			);
			outputContext.Writer.WriteLine(
				$"{indent}\t\tOnNormalizing{normalizeValidateSuffix}(ref __itemsValue);"
			);
			outputContext.Writer.WriteLine(
				$"{indent}\t\tOnValidating{normalizeValidateSuffix}(__itemsValue);"
			);
			outputContext.Writer.WriteLine(
				$"{indent}\t\tvar __eventItems = __itemsValue as {collectionEvent.ElementTypeName}[] ?? [.. __itemsValue];"
			);
			outputContext.Writer.WriteLine($"{indent}\t\tif (__eventItems.Length == 0)");
			outputContext.Writer.WriteLine($"{indent}\t\t{{");
			EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
			outputContext.Writer.WriteLine($"{indent}\t\t}}");

			var isAddOperation = collectionEvent.Operation == CollectionMutationOperation.Add;
			if (isAddOperation && collectionEvent.IsSet)
			{
				outputContext.Writer.WriteLine();
				outputContext.Writer.WriteLine($"{indent}\t\tvar __hasNewValues = false;");
				outputContext.Writer.WriteLine($"{indent}\t\tforeach (var __item in __eventItems)");
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				outputContext.Writer.WriteLine(
					$"{indent}\t\t\tif (!{collectionEvent.PropertyName}.Contains(__item))"
				);
				outputContext.Writer.WriteLine($"{indent}\t\t\t{{");
				outputContext.Writer.WriteLine($"{indent}\t\t\t\t__hasNewValues = true;");
				outputContext.Writer.WriteLine($"{indent}\t\t\t\tbreak;");
				outputContext.Writer.WriteLine($"{indent}\t\t\t}}");
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
				outputContext.Writer.WriteLine($"{indent}\t\tif (!__hasNewValues)");
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
			}
			else if (!isAddOperation)
			{
				outputContext.Writer.WriteLine();
				outputContext.Writer.WriteLine($"{indent}\t\tvar __hasExistingValues = false;");
				outputContext.Writer.WriteLine($"{indent}\t\tforeach (var __item in __eventItems)");
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				outputContext.Writer.WriteLine(
					$"{indent}\t\t\tif ({collectionEvent.PropertyName}.Contains(__item))"
				);
				outputContext.Writer.WriteLine($"{indent}\t\t\t{{");
				outputContext.Writer.WriteLine($"{indent}\t\t\t\t__hasExistingValues = true;");
				outputContext.Writer.WriteLine($"{indent}\t\t\t\tbreak;");
				outputContext.Writer.WriteLine($"{indent}\t\t\t}}");
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
				outputContext.Writer.WriteLine($"{indent}\t\tif (!__hasExistingValues)");
				outputContext.Writer.WriteLine($"{indent}\t\t{{");
				EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
				outputContext.Writer.WriteLine($"{indent}\t\t}}");
			}

			outputContext.Writer.WriteLine();
			outputContext.Writer.WriteLine(
				$"{indent}\t\tvar @event = new global::{method.EventNamespace}.{method.EventName}"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t{{");
			outputContext.Writer.WriteLine(
				$"{indent}\t\t\t{parameter.PropertyName} = __eventItems,"
			);
			outputContext.Writer.WriteLine($"{indent}\t\t}};");
			outputContext.Writer.WriteLine($"{indent}\t\tif (!ShouldApply{hookSuffix}(@event))");
			outputContext.Writer.WriteLine($"{indent}\t\t{{");
			EmitNoChangeReturn(outputContext, method.ReturnKind, indent, 3);
			outputContext.Writer.WriteLine($"{indent}\t\t}}");
			outputContext.Writer.WriteLine();
			outputContext.Writer.WriteLine($"{indent}\t\tOnRaising{hookSuffix}(ref __itemsValue);");
		}

		static void EmitCollectionFinalization(
			AggregateGenerationOutputContext outputContext,
			AggregateEventMethodInfo method,
			string hookSuffix,
			string indent
		)
		{
			outputContext.Writer.WriteLine();
			outputContext.Writer.WriteLine($"{indent}\t\tOnRaised{hookSuffix}(@event);");
			outputContext.Writer.WriteLine($"{indent}\t\tRecordAndApply(@event);");
			outputContext.Writer.WriteLine();
			EmitSuccessReturn(outputContext, method.ReturnKind, indent, 2);
		}

		static void EmitCollectionTrailingHookDeclarations(
			AggregateGenerationOutputContext outputContext,
			AggregateEventMethodInfo method,
			CollectionEventInfo collectionEvent,
			EventPropertyInfo parameter,
			string hookSuffix,
			string indent
		)
		{
			EmitCa1822Suppression(outputContext, indent);
			if (collectionEvent.ParameterShape == CollectionParameterShape.Single)
				outputContext.Writer.WriteLine(
					$"{indent}\tpartial void OnRaising{hookSuffix}(ref {collectionEvent.ElementTypeName} {parameter.ParameterName});"
				);
			else
				outputContext.Writer.WriteLine(
					$"{indent}\tpartial void OnRaising{hookSuffix}(ref global::System.Collections.Generic.IEnumerable<{collectionEvent.ElementTypeName}> {parameter.ParameterName});"
				);

			EmitCa1822Suppression(outputContext, indent);
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
			EmitCa1822Suppression(outputContext, indent);
			outputContext.Writer.WriteLine(
				$"{indent}\tpartial void OnShouldApply{hookSuffix}(global::{method.EventNamespace}.{method.EventName} @event, ref bool shouldApply);"
			);
			EmitCa1822Suppression(outputContext, indent);
			outputContext.Writer.WriteLine(
				$"{indent}\tpartial void OnRaised{hookSuffix}(global::{method.EventNamespace}.{method.EventName} @event);"
			);
			EmitCa1822Suppression(outputContext, indent);
			outputContext.Writer.WriteLine(
				$"{indent}\tpartial void OnApplied{hookSuffix}(global::{method.EventNamespace}.{method.EventName} @event);"
			);
			outputContext.Writer.WriteLine();
		}
	}
}
