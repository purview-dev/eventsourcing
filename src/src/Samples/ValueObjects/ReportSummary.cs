using Purview.EventSourcing.Samples.Domain.ReportUpload;
using Purview.EventSourcing.Serialization;

namespace Purview.EventSourcing.Samples.ValueObjects;

[Scalar]
public sealed partial record class ReportSummary
{
	public ParserReportSummary Value { get; }
}
