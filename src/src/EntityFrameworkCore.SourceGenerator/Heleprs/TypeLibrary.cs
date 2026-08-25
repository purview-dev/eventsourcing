namespace Purview.EventSourcing.EntityFrameworkCore.SourceGenerator.Heleprs;

static class TypeLibrary
{
	public const string RootNamespace = "Purview.EventSourcing.EntityFrameworkCore";

	public static readonly TypeIdentity EFOpaqueAttribute = new(nameof(EFOpaqueAttribute), RootNamespace);

	public static readonly TypeIdentity AggregateAttribute = new(
		nameof(AggregateAttribute),
		"Purview.EventSourcing.Aggregates"
	);

	public static readonly TypeIdentity DictionaryKV = new(typeof(Dictionary<,>));

	public static readonly TypeIdentity IDictionaryKV = new(typeof(IDictionary<,>));

	public static readonly TypeIdentity IReadOnlyDictionaryKV = new(typeof(IReadOnlyDictionary<,>));
}
