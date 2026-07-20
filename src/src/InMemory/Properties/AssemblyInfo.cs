using System.Runtime.CompilerServices;
using Purview.Telemetry;

[assembly: InternalsVisibleTo("EventSourcing.UnitTests")]

[assembly: ActivitySourceGeneration("Purview.EventSourcing.InMemory")]
[assembly: MeterGeneration("Purview.EventSourcing.InMemory")]
