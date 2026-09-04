using System.Runtime.CompilerServices;
using Purview.Telemetry;

[assembly: InternalsVisibleTo("Purview.EventSourcing.SharedTestingFramework")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.UnitTests")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.Admin.MongoDB")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.Admin.MongoDB.UnitTests")]

[assembly: ActivitySourceGeneration("Purview.EventSourcing.MongoDB")]
[assembly: MeterGeneration("Purview.EventSourcing.MongoDB")]
