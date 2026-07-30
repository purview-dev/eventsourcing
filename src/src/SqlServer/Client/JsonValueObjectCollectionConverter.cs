using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Purview.EventSourcing.Serialization;

namespace Purview.EventSourcing.SqlServer.Client;

sealed class JsonValueObjectCollectionConverter<TCollection> : ValueConverter<TCollection, string>
{
	public JsonValueObjectCollectionConverter()
		: base(
			value => EventStoreSerializationHelpers.Serialize(value),
			value => EventStoreSerializationHelpers.Deserialize<TCollection>(value)!
		)
	{ }
}
