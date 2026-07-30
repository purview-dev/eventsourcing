using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Admin.Api;
using TUnit.Core;

namespace Purview.EventSourcing.Admin.UnitTests.Api;

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
			if (!ex.Message.Contains("RoutePrefix"))
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
			if (!ex.Message.Contains("must start with"))
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
			if (!ex.Message.Contains("DefaultPageSize"))
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
			if (!ex.Message.Contains("DefaultPageSize"))
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
