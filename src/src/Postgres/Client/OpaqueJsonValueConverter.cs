using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Purview.EventSourcing.Serialization;

namespace Purview.EventSourcing.Postgres.Client;

/// <summary>
/// Converts a value marked with the EF opaque attribute to and from its JSON-serialized string
/// representation for storage in PostgreSQL.
/// </summary>
/// <typeparam name="TValue">The opaque value type being converted.</typeparam>
sealed class OpaqueJsonValueConverter<TValue> : ValueConverter<TValue, string>
{
	/// <summary>
	/// Creates a new <see cref="OpaqueJsonValueConverter{TValue}"/>.
	/// </summary>
	public OpaqueJsonValueConverter()
		: base(ToJsonExpression(), FromJsonExpression()) { }

	static Expression<Func<TValue, string>> ToJsonExpression() =>
		value => EventStoreSerializationHelpers.Serialize(value);

	static Expression<Func<string, TValue>> FromJsonExpression() =>
		value => EventStoreSerializationHelpers.Deserialize<TValue>(value)!;
}
