namespace Purview.EventSourcing.SourceGenerator.Aggregate;

partial class AggregateSourceEmitter
{
	static void GenerateEventClass(AggregateEmitContext outputContext, AggregateEventMethodInfo method)
	{
		outputContext.Debug(
			$"Generating event class '{method.EventType}' for method '{method.MethodName}' with {method.EventParameters.Length} stored parameters and version {method.Version}."
		);

		var hashParameterName = method.EventParameters.Length == 0 ? "_" : "hash";

		outputContext.Writer.WriteClass(
			new(method.EventType.Identity.Name, TypeDeclarationAccessibility.Public)
			{
				IsSealed = true,
				IsPartial = false,
				BaseType = TypeLibrary.Aggregates.EventBase,
			},
			bodyWriter =>
			{
				foreach (var prop in method.EventParameters)
				{
					var propertyType = prop.PropertyType;
					// TODO: not sure this is needed anymore...
					//if ((prop.IsNotNull || prop.IsRequired) && propertyType.IsNullable)
					//	propertyType = propertyType.Nullable();

					bodyWriter.WriteProperty(
						new(prop.PropertyName, propertyType, TypeDeclarationAccessibility.Public)
						{
							HasSetter = true,
							Initializer = "default!",
						}
					);
				}

				bodyWriter.WriteProperty(
					new("SchemaVersion", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
					{
						IsOverride = true,
						ExpressionBody = $"{method.Version}",
					}
				);

				bodyWriter.WriteMethod(
					new("BuildEventHash", TypeDeclarationAccessibility.Protected)
					{
						IsOverride = true,
						Parameters = [new(hashParameterName, TypeLibrary.System.HashCode, ParameterModifier.Ref)],
					},
					methodBodyWriter =>
					{
						foreach (var prop in method.EventParameters)
							methodBodyWriter
								.Write(hashParameterName)
								.Write('.')
								.WriteMethodCall("Add", prop.PropertyName);
					}
				);
			}
		);
	}
}
