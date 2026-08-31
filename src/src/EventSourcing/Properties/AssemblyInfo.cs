using System.Runtime.CompilerServices;
using Purview.Telemetry;

[assembly: InternalsVisibleTo("SharedTestingFramework")]
[assembly: InternalsVisibleTo("AzureStorage.IntegrationTests")]
[assembly: InternalsVisibleTo("CosmosDb.IntegrationTests")]
[assembly: InternalsVisibleTo("MongoDB.IntegrationTests")]
[assembly: InternalsVisibleTo("SqlServer.IntegrationTests")]

[assembly: InternalsVisibleTo("Purview.EventSourcing.ImplementationShared")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.InMemory")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.AzureStorage")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.CosmosDb")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.MongoDB")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.SqlServer")]

[assembly: ActivitySourceGeneration("Purview.EventSourcing")]
[assembly: MeterGeneration("Purview.EventSourcing")]
