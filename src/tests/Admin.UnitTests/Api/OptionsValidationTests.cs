using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Admin.Api;

public sealed class OptionsValidationTests
{
	[Test]
	public async Task AdminPortalOptions_Validate_ThrowsOnEmptyRoutePrefix()
	{
		// Arrange
		var options = new AdminPortalOptions { RoutePrefix = "" };

		// Act & Assert
		await Assert.That(options.Validate).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task AdminPortalOptions_Validate_ThrowsIfRoutePrefixDoesNotStartWithSlash()
	{
		// Arrange
		var options = new AdminPortalOptions { RoutePrefix = "admin/api" };

		// Act & Assert
		await Assert.That(options.Validate).Throws<InvalidOperationException>();
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
	public async Task AdminPagingOptions_Validate_ThrowsIfDefaultPageSizeIsZero()
	{
		// Arrange
		var options = new AdminPagingOptions { DefaultPageSize = 0 };

		// Act & Assert
		await Assert.That(options.Validate).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task AdminPagingOptions_Validate_ThrowsIfDefaultPageSizeExceedsMax()
	{
		// Arrange
		var options = new AdminPagingOptions { DefaultPageSize = 1000, MaxPageSize = 500 };

		// Act & Assert
		await Assert.That(options.Validate).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task AdminPagingOptions_Validate_ThrowsIfMaxPageSizeIsLessThanOne()
	{
		// Arrange
		var options = new AdminPagingOptions { DefaultPageSize = 1, MaxPageSize = 0 };

		// Act & Assert
		await Assert.That(options.Validate).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task AdminProjectionOptions_Validate_ThrowsIfMaxVersionsPerQueryIsLessThanOne()
	{
		// Arrange
		var options = new AdminProjectionOptions { MaxVersionsPerQuery = 0 };

		// Act & Assert
		await Assert.That(options.Validate).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task AdminProjectionOptions_Validate_ThrowsIfMaxTimeRangePerQueryIsNegative()
	{
		// Arrange
		var options = new AdminProjectionOptions { MaxTimeRangePerQuery = TimeSpan.FromSeconds(-1) };

		// Act & Assert
		await Assert.That(options.Validate).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task AdminPortalOptions_ServiceCollection_ValidatesOnStart()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddPurviewEventSourcingAdminApi(opts => opts.RoutePrefix = "");

		var provider = services.BuildServiceProvider();
		AdminPortalOptions ResolveInvalidOptions() => provider.GetRequiredService<IOptions<AdminPortalOptions>>().Value;

		// Act & Assert
		await Assert.That(ResolveInvalidOptions).Throws<OptionsValidationException>();
	}
}
