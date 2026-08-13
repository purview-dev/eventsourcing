using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.SqlServer.Events;

sealed class SqlServerEventStoreOptionsValidator : IValidateOptions<SqlServerEventStoreOptions>
{
	public ValidateOptionsResult Validate(string? name, SqlServerEventStoreOptions options)
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
			_ = new SqlServerEventStoreClient(options);
			return ValidateOptionsResult.Success;
		}
		catch (ArgumentException ex)
		{
			return ValidateOptionsResult.Fail(ex.Message);
		}
	}
}
