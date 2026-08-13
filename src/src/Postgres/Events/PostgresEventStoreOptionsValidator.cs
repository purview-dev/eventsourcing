using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Postgres.Events;

sealed class PostgresEventStoreOptionsValidator : IValidateOptions<PostgresEventStoreOptions>
{
	public ValidateOptionsResult Validate(string? name, PostgresEventStoreOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		var validationContext = new ValidationContext(options);
		var validationResults = new List<ValidationResult>();
		if (
			!Validator.TryValidateObject(
				options,
				validationContext,
				validationResults,
				validateAllProperties: true
			)
		)
			return ValidateOptionsResult.Fail(
				validationResults.Select(static x => x.ErrorMessage ?? "Options validation failed.")
			);

		try
		{
			_ = new PostgresEventStoreClient(options);
			return ValidateOptionsResult.Success;
		}
		catch (ArgumentException ex)
		{
			return ValidateOptionsResult.Fail(ex.Message);
		}
	}
}
