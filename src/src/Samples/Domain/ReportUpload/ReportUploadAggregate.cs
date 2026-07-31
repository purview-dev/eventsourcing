using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Samples.ValueObjects;

namespace Purview.EventSourcing.Samples.Domain.ReportUpload;

[GenerateAggregate]
public sealed partial class ReportUploadAggregate
{
	public string OriginalFilename { get; private set; } = string.Empty;

	public string FileHash { get; private set; } = string.Empty;

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Purview.EventSourcing.SourceGenerator",
		"EVENTSTORE020",
		Justification = "Sample aggregate does not execute deep SQL predicates over BlobUri.Value."
	)]
	public BlobUri SourceJsonBlob { get; private set; } = BlobUri.Empty;

	public EventStoreSet<ProjectBlobs> ExcelReportBlobs { get; private set; } = [];

	public UserCapture Uploaded { get; private set; } = UserCapture.Empty;

	public ReportProcessingStatus Status { get; private set; } = ReportProcessingStatus.None;

	public string? FailureReason { get; private set; }

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Purview.EventSourcing.SourceGenerator",
		"EVENTSTORE020",
		Justification = "Query translation uses ReportSummaryScalar mirror for SQL-safe filtering."
	)]
	public ReportSummary? ReportSummary { get; private set; }

	public ParserReportSummary? ReportSummaryScalar { get; private set; }

	public ReportUploadAggregate MarkAsProcessing() => SetReportProcessingStatus(ReportProcessingStatus.Processing);

	public ReportUploadAggregate AddExcelReport(GuidObjectId projectId, BlobUri excelReportBlob) =>
		AddExcelReport(new ProjectBlobs(projectId, excelReportBlob));

	// Event generation methods
	[GenerateAggregateEvent(EventName = "MarkAsCompleted")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Purview.EventSourcing.SourceGenerator",
		"EVENTSTORE016:Event parameter nullability differs from aggregate property",
		Justification = "Required properties"
	)]
	public partial ReportUploadAggregate MarkAsComplete(
		ReportSummary reportSummary,
		[Computed] ParserReportSummary? reportSummaryScalar = null,
		[Computed] ReportProcessingStatus status = default
	);

	[GenerateAggregateCollectionEvent(nameof(ExcelReportBlobs))]
	private partial ReportUploadAggregate AddExcelReport(ProjectBlobs projectBlobs);

	[GenerateAggregateEvent(EventName = "MarkAsFailed")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Purview.EventSourcing.SourceGenerator",
		"EVENTSTORE014:Event name overrides should be past tense"
	)]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Purview.EventSourcing.SourceGenerator",
		"EVENTSTORE016:Event parameter nullability differs from aggregate property",
		Justification = "Required properties"
	)]
	public partial ReportUploadAggregate MarkAsFailed(
		string failureReason,
		ReportSummary? reportSummary = null,
		[Computed] ParserReportSummary? reportSummaryScalar = null,
		[Computed] ReportProcessingStatus status = default
	);

	[GenerateAggregateEvent]
	public partial ReportUploadAggregate Create(
		string originalFilename,
		string fileHash,
		BlobUri sourceJsonBlob,
		UserCapture uploaded
	);

	[GenerateAggregateEvent]
	private partial ReportUploadAggregate SetReportProcessingStatus(ReportProcessingStatus status);
}
