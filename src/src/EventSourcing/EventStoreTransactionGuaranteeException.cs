namespace Purview.EventSourcing;

/// <summary>
/// The enlisted stores cannot provide the transaction guarantee required by the caller.
/// </summary>
public sealed class EventStoreTransactionGuaranteeException : InvalidOperationException
{
	/// <summary>
	/// Creates an exception for an unavailable transaction guarantee.
	/// </summary>
	public EventStoreTransactionGuaranteeException(
		EventStoreTransactionGuarantee requiredGuarantee,
		EventStoreTransactionGuarantee availableGuarantee
	)
		: base(
			$"The transaction requires the '{requiredGuarantee}' guarantee, but the enlisted stores provide '{availableGuarantee}'. No saves were attempted."
		)
	{
		RequiredGuarantee = requiredGuarantee;
		AvailableGuarantee = availableGuarantee;
	}

	/// <summary>Gets the guarantee requested by the caller.</summary>
	public EventStoreTransactionGuarantee RequiredGuarantee { get; }

	/// <summary>Gets the strongest guarantee available from the enlisted stores.</summary>
	public EventStoreTransactionGuarantee AvailableGuarantee { get; }
}
