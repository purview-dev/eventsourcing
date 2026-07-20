using Purview.EventSourcing.Guards;
using Purview.EventSourcing.Samples.Domain.ReportUpload.ReportUploadEvents;
using Purview.EventSourcing.Samples.ValueObjects;

namespace Purview.EventSourcing.Samples.Domain.ReportUpload;

partial class ReportUploadAggregate
{
	// Property rules
	partial void OnOriginalFilenameChanging(ref string originalFilename) => originalFilename.Required(true);

	partial void OnFileHashChanging(ref string fileHash) => fileHash.Required(true);

	partial void OnSourceJsonBlobChanging(ref BlobUri sourceJsonBlob) => sourceJsonBlob.Required();

	// Event rules
	partial void OnRaisingCreatedEvent(
		ref string originalFilename,
		ref string fileHash,
		ref BlobUri sourceJsonBlob,
		ref UserCapture uploaded
	) => this.MustBeNew();

	partial void OnUploadedChanging(ref UserCapture uploaded)
	{
		if (Details.CurrentVersion > 0 && Uploaded.IsEssentialChange(uploaded))
			throw new InvalidOperationException("Cannot change uploaded information after report creation.");
	}

	partial void OnComputingMarkAsCompletedEvent(ref ReportProcessingStatus status) =>
		status = ReportProcessingStatus.Completed;

	partial void OnRaisingMarkAsCompletedEvent(ref ReportSummary? reportSummary) => reportSummary.Required();

	partial void OnComputingMarkAsFailedEvent(ref ReportProcessingStatus status) =>
		status = ReportProcessingStatus.Failed;

	partial void OnRaisingMarkAsFailedEvent(ref string? failureReason, ref ReportSummary? reportSummary) =>
		failureReason.Required(true);

	partial void OnAppliedMarkAsFailedEvent(MarkAsFailed @event) => ExcelReportBlobs = [];

	partial void OnRaisingReportProcessingStatusSetEvent(ref ReportProcessingStatus status)
	{
		if (status == ReportProcessingStatus.Completed)
			throw new InvalidOperationException(
				$"Use the {nameof(MarkAsComplete)} method to set the report as complete."
			);
		else if (status == ReportProcessingStatus.Failed)
			throw new InvalidOperationException($"Use the {nameof(MarkAsFailed)} method to set the report as failed.");
	}
}
