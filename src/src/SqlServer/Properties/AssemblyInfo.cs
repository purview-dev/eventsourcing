using System.Runtime.CompilerServices;
using Purview.Telemetry;

[assembly: InternalsVisibleTo("SharedTestingFramework")]
[assembly: InternalsVisibleTo("Purview.EventSourcing.UnitTests")]

[assembly: ActivitySourceGeneration("Purview.EventSourcing.SqlServer")]
[assembly: MeterGeneration("Purview.EventSourcing.SqlServer")]
