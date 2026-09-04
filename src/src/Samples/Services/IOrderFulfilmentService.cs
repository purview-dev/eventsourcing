namespace Purview.EventSourcing.Samples.Services;

public interface IOrderFulfilmentService
{
	Task<FulfilmentResult> PlaceOrderAsync(
		string customerId,
		string inventoryId,
		int quantity,
		string? shippingAddress,
		CancellationToken cancellationToken = default
	);
}
