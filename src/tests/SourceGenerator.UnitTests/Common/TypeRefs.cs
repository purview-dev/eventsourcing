using Purview.SourceGeneratorFramework;

namespace Purview.EventSourcing.SourceGenerator.Common;

/// <summary>
/// Shared <see cref="TypeReference"/> building blocks for CodeQuery-based assertions.
/// Reference-type nullability is metadata for matching purposes, so plain <c>string</c>
/// references also match <c>string?</c> parameters.
/// </summary>
static class TypeRefs
{
	public static readonly TypeReference String = TypeReference.Create<string>();

	public static readonly TypeReference Int = TypeReference.Create<int>();

	public static readonly TypeReference Decimal = TypeReference.Create<decimal>();

	public static readonly TypeReference Bool = TypeReference.Create<bool>();

	public static readonly TypeReference Object = TypeReference.Create<object>();

	public static readonly TypeReference Guid = TypeReference.Create<Guid>();

	public static readonly TypeReference HashCode = TypeReference.Create<HashCode>();

	public static TypeReference Event(string name, string @namespace) => Named(name, @namespace);

	public static TypeReference Named(string name, string @namespace) => new(new TypeIdentity(name, @namespace));

	public static TypeReference EnumerableOf(TypeReference element) =>
		new(new TypeIdentity("IEnumerable", "System.Collections.Generic").MakeGeneric(element));

	public static TypeReference ICollectionOf(TypeReference element) =>
		new(new TypeIdentity("ICollection", "System.Collections.Generic").MakeGeneric(element));
}
