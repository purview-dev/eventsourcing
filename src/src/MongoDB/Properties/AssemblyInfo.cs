using System.Runtime.CompilerServices;
using Purview.Telemetry;

[assembly: InternalsVisibleTo("SharedTestingFramework")]
[assembly: InternalsVisibleTo("EventSourcing.UnitTests")]
[assembly: InternalsVisibleTo("Admin.MongoDB")]
[assembly: InternalsVisibleTo("Admin.MongoDB.UnitTests")]

[assembly: ActivitySourceGeneration("Purview.EventSourcing.MongoDB")]
[assembly: MeterGeneration("Purview.EventSourcing.MongoDB")]
