using Microsoft.EntityFrameworkCore;
using Npgsql;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.Fixtures.Postgres;

namespace Purview.EventSourcing.Postgres.Snapshots;

[ClassDataSource<PostgresSnapshotEventStoreFixture>(Shared = SharedType.PerTestSession)]
public sealed class PostgresProviderIntegrationTests(PostgresSnapshotEventStoreFixture fixture)
{
	[Test]
	public async Task EventStore_SaveAndReplay_RestoresAggregateState(CancellationToken cancellationToken)
	{
		var store = fixture.CreateEventStore<PersistenceAggregate>();
		var aggregate = await store.CreateAsync(cancellationToken: cancellationToken);
		aggregate.SetInt32Value(123);
		aggregate.AppendString("hello");

		await store.SaveAsync(aggregate, cancellationToken: cancellationToken);

		var rehydrated = await store.GetAsync(aggregate.Details.Id, cancellationToken: cancellationToken);
		await Assert.That(rehydrated).IsNotNull();
		await Assert.That(rehydrated!.Int32Value).IsEqualTo(123);
		await Assert.That(rehydrated.StringProperty).Contains("hello");
	}

	[Test]
	public async Task SnapshotStore_UpsertAndLinqQuery_WorksWithNestedPayload(CancellationToken cancellationToken)
	{
		var store = fixture.CreateSnapshotStore<PersistenceAggregate>();
		var aggregate = new PersistenceAggregate { Details = { Id = Guid.NewGuid().ToString("D") } };
		aggregate.SetComplexProperty(new() { Int32Property = 42, StringProperty = "active" });

		await store.SnapshotAsync(aggregate, cancellationToken);

		var response = await store.QueryAsync(
			a => a.ComplexTestType != null && a.ComplexTestType.Int32Property == 42,
			cancellationToken: cancellationToken
		);

		await Assert.That(response.Results).Count().IsEqualTo(1);
		await Assert.That(response.Results[0].Details.Id).IsEqualTo(aggregate.Details.Id);
	}

	[Test]
	public async Task SnapshotStore_WherePayloadContainsAndHasKey_QueryByJsonOperators(
		CancellationToken cancellationToken
	)
	{
		var store = fixture.CreateSnapshotStore<PersistenceAggregate>();
		var aggregate = new PersistenceAggregate { Details = { Id = Guid.NewGuid().ToString("D") } };
		aggregate.AddKVPs(new KeyValuePair<string, string>("status", "active"));
		aggregate.SetComplexProperty(new() { StringProperty = "active" });

		await store.SnapshotAsync(aggregate, cancellationToken);

		var containsResponse = await store.WherePayloadContainsAsync(
			/*lang=json,strict*/
			"""{"ComplexTestType":{"StringProperty":"active"}}""",
			new ContinuationRequest { MaxRecords = 10 },
			cancellationToken
		);
		var keyResponse = await store.WherePayloadHasKeyAsync(
			"ComplexTestType",
			new ContinuationRequest { MaxRecords = 10 },
			cancellationToken
		);

		await Assert.That(containsResponse.Results).Count().IsEqualTo(1);
		await Assert.That(keyResponse.Results).Count().IsEqualTo(1);
	}

	[Test]
	public async Task ExecuteUpdate_PartialJsonUpdate_UpdatesSingleField(CancellationToken cancellationToken)
	{
		var tableName = $"exec_update_{Guid.NewGuid():N}";
		var options = new DbContextOptionsBuilder<ExecuteUpdateDbContext>().UseNpgsql(fixture.ConnectionString).Options;

		await using (var setupContext = new ExecuteUpdateDbContext(options, tableName))
		{
			await setupContext.Database.EnsureCreatedAsync(cancellationToken);
			setupContext.Rows.Add(
				new ExecuteUpdateRow
				{
					Id = Guid.NewGuid().ToString("D"),
					State = new ExecuteUpdateState { Version = 1, Status = "pending" },
				}
			);
			await setupContext.SaveChangesAsync(cancellationToken);
		}

		await using (var updateContext = new ExecuteUpdateDbContext(options, tableName))
		{
			await updateContext
				.Rows.Where(static r => r.State.Status == "pending")
				.ExecuteUpdateAsync(
					s => s.SetProperty(static r => r.State.Version, static r => r.State.Version + 1),
					cancellationToken
				);
		}

		await using var verifyContext = new ExecuteUpdateDbContext(options, tableName);
		var row = await verifyContext.Rows.SingleAsync(cancellationToken);
		await Assert.That(row.State.Version).IsEqualTo(2);
		await Assert.That(row.State.Status).IsEqualTo("pending");
	}

	[Test]
	public async Task AutoCreate_CreatesGinAndExpressionIndexes(CancellationToken cancellationToken)
	{
		var runId = Guid.NewGuid();
		var tableName = $"EventStoreSnapshots_{runId:N}";
		var store = fixture.CreateSnapshotStore<PersistenceAggregate>(
			runId: runId,
			configureOptions: options =>
				options.JsonIndexOptions = new PostgresJsonIndexOptions
				{
					Enabled = true,
					UseJsonbPathOps = true,
					PathIndexes = [new() { Path = "ComplexTestType.StringProperty" }],
				}
		);
		var aggregate = new PersistenceAggregate { Details = { Id = Guid.NewGuid().ToString("D") } };
		aggregate.SetComplexProperty(new() { StringProperty = "indexed" });
		await store.SnapshotAsync(aggregate, cancellationToken);

		await using var connection = new NpgsqlConnection(fixture.ConnectionString);
		await connection.OpenAsync(cancellationToken);
		await using var command = new NpgsqlCommand(
			"""
			SELECT indexdef
			FROM pg_indexes
			WHERE schemaname = 'public' AND upper(tablename) = @tableName;
			""",
			connection
		);
		command.Parameters.AddWithValue("tableName", tableName.ToUpperInvariant());

		var indexDefinitions = new List<string>();
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
			indexDefinitions.Add(reader.GetString(0));

		await Assert
			.That(indexDefinitions.Any(def => def.Contains("USING gin", StringComparison.OrdinalIgnoreCase)))
			.IsTrue();
		await Assert.That(indexDefinitions.Any(def => def.Contains("#>>", StringComparison.Ordinal))).IsTrue();
	}

	sealed class ExecuteUpdateDbContext(DbContextOptions<ExecuteUpdateDbContext> options, string tableName)
		: DbContext(options)
	{
		readonly string _tableName = tableName;

		public DbSet<ExecuteUpdateRow> Rows => Set<ExecuteUpdateRow>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<ExecuteUpdateRow>(entity =>
			{
				entity.ToTable(_tableName, "public");
				entity.HasKey(static x => x.Id);
				entity.ComplexProperty(static x => x.State, static x => x.ToJson());
			});
		}
	}

	sealed class ExecuteUpdateRow
	{
		public required string Id { get; set; }

		public required ExecuteUpdateState State { get; set; }
	}

	sealed class ExecuteUpdateState
	{
		public int Version { get; set; }

		public required string Status { get; set; }
	}
}
