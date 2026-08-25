using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Purview.EventSourcing.Postgres.Events.EntityFramework;

/// <summary>
/// Design-time factory for <see cref="EventStoreDbContext"/>.
/// Used by EF Core tools to generate migrations.
/// </summary>
public sealed class EventStoreDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
{
	public EventStoreDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
		optionsBuilder.UseNpgsql("Host=localhost;Database=eventstore_design;Username=postgres;Password=postgres");

		return new EventStoreDbContext(optionsBuilder.Options);
	}
}
