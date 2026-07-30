using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Purview.EventSourcing.Admin.SqlServer.UnitTests;

public class SqlServerAdminProjectionServiceTests
{
	[Test]
	public async Task ProjectionService_IsRegisterable_InDependencyContainer()
	{
		// Arrange
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var options = Microsoft.Extensions.Options.Options.Create(
			new Purview.EventSourcing.SqlServer.Events.SqlServerEventStoreOptions { ConnectionString = "test" }
		);

		services.AddSingleton(options);
		services.AddTransient<Purview.EventSourcing.Admin.SqlServer.SqlServerAdminProjectionService>();

		var provider = services.BuildServiceProvider();

		// Act
		var service =
			provider.GetRequiredService<Purview.EventSourcing.Admin.SqlServer.SqlServerAdminProjectionService>();

		// Assert
		await Assert.That(service).IsNotNull();
	}

	[Test]
	public async Task ProjectionService_Implements_IAdminProjectionService()
	{
		// Arrange & Act
		var service = typeof(Purview.EventSourcing.Admin.SqlServer.SqlServerAdminProjectionService);
		var interfaceType = typeof(Purview.EventSourcing.Admin.Abstractions.IAdminProjectionService);

		// Assert
		await Assert.That(service.GetInterfaces()).Contains(interfaceType);
	}
}
