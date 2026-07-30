using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Admin.Api;

public sealed class OptionsValidationTests
{
	[Test]
	public void AdminPortalOptions_Validate_ThrowsOnEmptyRoutePrefix()
	{
		// Arrange
		var options = new AdminPortalOptions { RoutePrefix = "" };

		// Act & Assert
		try
		{
			options.Validate();
			throw new InvalidOperationException("Should have thrown");
		}
		catch (InvalidOperationException ex)
		{
			if (!ex.Message.Contains("RoutePrefix", StringComparison.Ordinal))
				throw;
		}
	}

	[Test]
	public void AdminPortalOptions_Validate_ThrowsIfRoutePrefixDoesNotStartWithSlash()
	{
		// Arrange
		var options = new AdminPortalOptions { RoutePrefix = "admin/api" };

		// Act & Assert
		try
		{
			options.Validate();
			throw new InvalidOperationException("Should have thrown");
		}
		catch (InvalidOperationException ex)
		{
			if (!ex.Message.Contains("must start with", StringComparison.Ordinal))
				throw;
		}
	}

	[Test]
	public void AdminPortalOptions_Validate_SucceedsWithValidRoutePrefix()
	{
		// Arrange
		var options = new AdminPortalOptions { RoutePrefix = "/admin/api" };

		// Act & Assert - should not throw
		options.Validate();
	}

	[Test]
	public void AdminPagingOptions_Validate_ThrowsIfDefaultPageSizeIsZero()
	{
		// Arrange
		var options = new AdminPagingOptions { DefaultPageSize = 0 };

		// Act & Assert
		try
		{
			options.Validate();
			throw new InvalidOperationException("Should have thrown");
		}
		catch (InvalidOperationException ex)
		{
			if (!ex.Message.Contains("DefaultPageSize", StringComparison.Ordinal))
				throw;
		}
	}

	[Test]
	public void AdminPagingOptions_Validate_ThrowsIfDefaultPageSizeExceedsMax()
	{
		// Arrange
		var options = new AdminPagingOptions { DefaultPageSize = 1000, MaxPageSize = 500 };

		// Act & Assert
		try
		{
			options.Validate();
			throw new InvalidOperationException("Should have thrown");
		}
		catch (InvalidOperationException ex)
		{
			if (!ex.Message.Contains("DefaultPageSize", StringComparison.Ordinal))
				throw;
		}
	}

	[Test]
	public void AdminPortalOptions_ServiceCollection_ValidatesOnStart()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddPurviewEventSourcingAdminApi(opts => opts.RoutePrefix = "");

		var provider = services.BuildServiceProvider();

		// Act & Assert
		try
		{
			_ = provider.GetRequiredService<IOptions<AdminPortalOptions>>().Value;
			throw new InvalidOperationException("Should have thrown");
		}
		catch (OptionsValidationException)
		{
			// Expected
		}
	}
}
