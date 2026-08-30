using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Purview.EventSourcing.Postgres.Snapshots.EntityFramework;

/// <summary>
/// Design-time factory for <see cref="SnapshotStoreDbContext"/>.
/// Used by EF Core tools to generate migrations.
/// </summary>
public sealed class SnapshotStoreDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SnapshotStoreDbContext>
{
	/// <summary>
	/// Creates a new <see cref="SnapshotStoreDbContext"/> for design-time tooling.
	/// </summary>
	/// <param name="args">Command-line arguments passed by EF Core design-time tools.</param>
	/// <returns>A new <see cref="SnapshotStoreDbContext"/> instance.</returns>
	public SnapshotStoreDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<SnapshotStoreDbContext>();
		optionsBuilder.UseNpgsql("Host=localhost;Database=snapshotstore_design;Username=postgres;Password=postgres");

		return new SnapshotStoreDbContext(optionsBuilder.Options);
	}
}
