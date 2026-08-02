using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Abstractions.Services;
using TUnit.Core;

namespace Purview.EventSourcing.Admin.SqlServer;

public class SqlServerAdminProjectionServiceTests
{
	[Test]
	public async Task ProjectionService_IsRegisterable_InDependencyContainer()
	{
		// Arrange
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var options = Microsoft.Extensions.Options.Options.Create(
			new EventSourcing.SqlServer.Events.SqlServerEventStoreOptions { ConnectionString = "test" }
		);

		services.AddSingleton(options);
		services.AddTransient<SqlServerAdminProjectionService>();

		var provider = services.BuildServiceProvider();

		// Act
		var service = provider.GetRequiredService<SqlServerAdminProjectionService>();

		// Assert
		await Assert.That(service).IsNotNull();
	}

	[Test]
	public async Task ProjectionService_Implements_IAdminProjectionService()
	{
		// Arrange & Act
		var service = typeof(SqlServerAdminProjectionService);
		var interfaceType = typeof(IAdminProjectionService);

		// Assert
		await Assert.That(service.GetInterfaces()).Contains(interfaceType);
	}
}
