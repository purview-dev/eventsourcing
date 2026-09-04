using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Purview.EventSourcing.Serialization;

namespace Purview.EventSourcing.SqlServer.Client;

sealed class OpaqueJsonValueConverter<TValue> : ValueConverter<TValue, string>
{
	public OpaqueJsonValueConverter()
		: base(ToJsonExpression(), FromJsonExpression()) { }

	static Expression<Func<TValue, string>> ToJsonExpression() =>
		value => EventStoreSerializationHelpers.Serialize(value);

	static Expression<Func<string, TValue>> FromJsonExpression() =>
		value => EventStoreSerializationHelpers.Deserialize<TValue>(value)!;
}
