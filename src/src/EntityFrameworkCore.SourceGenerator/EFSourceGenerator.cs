using Microsoft.CodeAnalysis;
using Purview.EventSourcing.EntityFrameworkCore.SourceGenerator.Emitters;

namespace Purview.EventSourcing.EntityFrameworkCore.SourceGenerator;

[Generator]
public sealed class EFSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterEmbeddedAttribute(typeof(EFSourceGenerator).FullName, AssemblyInfo.Version);
		context.RegisterPostInitializationOutput(static output =>
		{
			foreach (var (HintName, Source) in AttributeEmiiter.GetAttributeSources())
				output.AddSource(HintName, Source);
		});
	}
}
