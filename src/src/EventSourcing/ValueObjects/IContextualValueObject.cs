namespace Purview.EventSourcing.ValueObjects;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1005:Avoid excessive parameters on generic types"
)]
public interface IContextualValueObject<TSelf, TValue, TAggregate>
	where TSelf : IValueObject
{
	static abstract TSelf Create(TValue value, in ValueObjectContext<TAggregate> context);
}
