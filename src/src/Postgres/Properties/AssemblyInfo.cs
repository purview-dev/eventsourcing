using System.Runtime.CompilerServices;
using Purview.Telemetry;

[assembly: InternalsVisibleTo("SharedTestingFramework")]
[assembly: InternalsVisibleTo("EventSourcing.UnitTests")]

[assembly: ActivitySourceGeneration("Purview.EventSourcing.Postgres")]
[assembly: MeterGeneration("Purview.EventSourcing.Postgres")]
