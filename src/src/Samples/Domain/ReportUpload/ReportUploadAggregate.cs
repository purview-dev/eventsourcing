using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Samples.ValueObjects;

namespace Purview.EventSourcing.Samples.Domain.ReportUpload;

[Aggregate]
public sealed partial class ReportUploadAggregate
{
	public string OriginalFilename { get; private set; } = string.Empty;

	public string FileHash { get; private set; } = string.Empty;

	public BlobUri SourceJsonBlob { get; private set; } = BlobUri.Empty;

	public EventStoreSet<ProjectBlobs> ExcelReportBlobs { get; private set; } = [];

	public UserCapture Uploaded { get; private set; } = UserCapture.Empty;

	public ReportProcessingStatus Status { get; private set; } = ReportProcessingStatus.None;

	public string? FailureReason { get; private set; }

	//[SuppressMessage(
	//	"Aggregates",
	//	"EVENTSTORE020",
	//	Justification = "Query translation uses ReportSummaryScalar mirror for SQL-safe filtering."
	//)]
	public ReportSummary? ReportSummary { get; private set; }

	public ParserReportSummary? ReportSummaryScalar { get; private set; }

	public ReportUploadAggregate MarkAsProcessing() =>
		SetReportProcessingStatus(ReportProcessingStatus.Processing);

	public ReportUploadAggregate AddExcelReport(GuidObjectId projectId, BlobUri excelReportBlob) =>
		AddExcelReport(new ProjectBlobs(projectId, excelReportBlob));

	// Event generation methods
	[Event(EventName = "MarkAsCompleted")]
	public partial ReportUploadAggregate MarkAsComplete(
		[NotNull] ReportSummary? reportSummary,
		[Computed] ParserReportSummary? reportSummaryScalar = null,
		[Computed] ReportProcessingStatus status = default
	);

	[CollectionEvent(nameof(ExcelReportBlobs))]
	private partial ReportUploadAggregate AddExcelReport(ProjectBlobs projectBlobs);

	[Event(EventName = "MarkAsFailed")]
	public partial ReportUploadAggregate MarkAsFailed(
		[Required] string? failureReason,
		ReportSummary? reportSummary = null,
		[Computed] ParserReportSummary? reportSummaryScalar = null,
		[Computed] ReportProcessingStatus status = default
	);

	[Event]
	public partial ReportUploadAggregate Create(
		string originalFilename,
		string fileHash,
		BlobUri sourceJsonBlob,
		UserCapture uploaded
	);

	[Event]
	private partial ReportUploadAggregate SetReportProcessingStatus(ReportProcessingStatus status);
}
