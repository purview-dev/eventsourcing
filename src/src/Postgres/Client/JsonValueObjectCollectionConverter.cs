using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Purview.EventSourcing.Serialization;

namespace Purview.EventSourcing.Postgres.Client;

/// <summary>
/// Converts a collection of value objects to and from its JSON-serialized string representation for storage in PostgreSQL.
/// </summary>
/// <typeparam name="TCollection">The collection type being converted.</typeparam>
sealed class JsonValueObjectCollectionConverter<TCollection> : ValueConverter<TCollection, string>
{
	/// <summary>
	/// Creates a new <see cref="JsonValueObjectCollectionConverter{TCollection}"/>.
	/// </summary>
	public JsonValueObjectCollectionConverter()
		: base(
			value => EventStoreSerializationHelpers.Serialize(value),
			value => EventStoreSerializationHelpers.Deserialize<TCollection>(value)!
		)
	{
		//
	}
}
