using System.ComponentModel.DataAnnotations;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Samples.ValueObjects;
using ZodSharp;

namespace Purview.EventSourcing.Samples.Domain;

/// <summary>
/// Simple aggregate demonstrating basic customer management.
/// Shows: single-property events, string manipulation, validation.
/// </summary>
[Aggregate]
[ZodSchema]
public sealed partial class CustomerAggregate : AggregateBase
{
	public Name Name { get; private set; }

	public EmailAddress Email { get; private set; }

	[StringLength(20)]
	public string? PhoneNumber { get; private set; }

	public bool IsActive { get; private set; }

	/// <summary>
	/// Updates one or more customer details in a single operation, raising a granular event
	/// for each field that has actually changed. Pass <see langword="null"/> for any field
	/// that should remain unchanged. To clear the phone number, use <see cref="ChangePhoneNumber"/> directly.
	/// </summary>
	public CustomerAggregate UpdateDetails(string? name = null, string? email = null, string? phoneNumber = null)
	{
		if (name is not null)
			ChangeName(name);

		if (email is not null)
			ChangeEmail(email);

		if (phoneNumber is not null)
			ChangePhoneNumber(phoneNumber);

		return this;
	}

	// Generated methods.

	[Event]
	public partial CustomerAggregate Deactivate();

	[Event]
	public partial CustomerAggregate Reactivate();

	[Event]
	public partial CustomerAggregate RegisterCustomer(string name, string email, bool isActive = true);

	[Event]
	public partial CustomerAggregate ChangeName(string name);

	[Event]
	public partial CustomerAggregate ChangeEmail(string email);

	[Event]
	public partial CustomerAggregate ChangePhoneNumber(string? phoneNumber);
}
