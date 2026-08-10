using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Site.Pages;

namespace Purview.EventSourcing.Samples.Admin.Site;

public sealed class AdminSiteEventsModelTests
{
	[Test]
	public async Task OnGetAsync_WithExistingAggregate_CallsQueryServiceAndReturnsPage(
		CancellationToken cancellationToken
	)
	{
		var expected = new PagedResult<EventEnvelopeResponse>(
			[
				new EventEnvelopeResponse(
					"CustomerAggregate",
					"customer-1",
					new EventMetadataResponse(
						1,
						DateTimeOffset.UtcNow,
						"CustomerRegistered",
						1,
						null,
						null,
						null,
						null
					),
					JsonDocument.Parse("{}").RootElement.Clone()
				),
			],
			1,
			25,
			1
		);

		var capturedAggregateType = (string?)null;
		var capturedAggregateId = (string?)null;
		EventRangeQuery? capturedQuery = null;

		var mockService = new MockAdminEventQueryService(
			(aggregateType, aggregateId, query, ct) =>
			{
				capturedAggregateType = aggregateType;
				capturedAggregateId = aggregateId;
				capturedQuery = query;
				return Task.FromResult<PagedResult<EventEnvelopeResponse>?>(expected);
			}
		);

		var model = new EventsModel(mockService) { AggregateType = "CustomerAggregate", AggregateId = "customer-1" };

		var result = await model.OnGetAsync(cancellationToken);

		await Assert.That(result).IsTypeOf<PageResult>();
		await Assert.That(model.EventRange).IsSameReferenceAs(expected);
		await Assert.That(capturedAggregateType).IsEqualTo("CustomerAggregate");
		await Assert.That(capturedAggregateId).IsEqualTo("customer-1");
		await Assert.That(capturedQuery).IsNotNull();
		await Assert.That(capturedQuery!.Page).IsEqualTo(1);
		await Assert.That(capturedQuery.PageSize).IsEqualTo(25);
	}

	[Test]
	public async Task OnGetAsync_WithEmptyEventStream_ReturnsPageAndEmptyResult(CancellationToken cancellationToken)
	{
		var expected = new PagedResult<EventEnvelopeResponse>([], 1, 25, 0);

		var mockService = new MockAdminEventQueryService(
			(_, _, _, _) => Task.FromResult<PagedResult<EventEnvelopeResponse>?>(expected)
		);

		var model = new EventsModel(mockService) { AggregateType = "CustomerAggregate", AggregateId = "customer-1" };

		var result = await model.OnGetAsync(cancellationToken);

		await Assert.That(result).IsTypeOf<PageResult>();
		await Assert.That(model.EventRange).IsNotNull();
		await Assert.That(model.EventRange!.Items).IsEmpty();
	}

	sealed class MockAdminEventQueryService(
		Func<string, string, EventRangeQuery, CancellationToken, Task<PagedResult<EventEnvelopeResponse>?>> handler
	) : IAdminEventQueryService
	{
		public Task<PagedResult<EventEnvelopeResponse>?> GetRangeAsync(
			string aggregateType,
			string aggregateId,
			EventRangeQuery query,
			CancellationToken cancellationToken
		) => handler(aggregateType, aggregateId, query, cancellationToken);
	}
}
