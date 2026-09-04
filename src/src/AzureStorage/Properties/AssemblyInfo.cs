using System.Runtime.CompilerServices;
using Purview.Telemetry;

[assembly: InternalsVisibleTo("Purview.EventSourcing.SharedTestingFramework")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.UnitTests")]

[assembly: ActivitySourceGeneration("Purview.EventSourcing.AzureStorage")]
[assembly: MeterGeneration("Purview.EventSourcing.AzureStorage")]
