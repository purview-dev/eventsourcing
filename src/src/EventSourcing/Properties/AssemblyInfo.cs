using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharedTestingFramework")]
[assembly: InternalsVisibleTo("AzureStorage.IntegrationTests")]
[assembly: InternalsVisibleTo("CosmosDb.IntegrationTests")]
[assembly: InternalsVisibleTo("MongoDB.IntegrationTests")]
[assembly: InternalsVisibleTo("SqlServer.IntegrationTests")]

[assembly: InternalsVisibleTo("ImplementationShared")]
[assembly: InternalsVisibleTo("InMemory")]
[assembly: InternalsVisibleTo("AzureStorage")]
[assembly: InternalsVisibleTo("CosmosDb")]
[assembly: InternalsVisibleTo("MongoDB")]
[assembly: InternalsVisibleTo("SqlServer")]
