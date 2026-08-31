using Purview.EventSourcing.Admin.Api.Contracts;

namespace Purview.EventSourcing.Admin.Api;

public sealed class AdminContractSchemaTests
{
	[Test]
	public async Task EventRangeRequestSchema_Validate_AcceptsDefaultRequest()
	{
		var result = EventRangeRequestSchema.Validate(new EventRangeRequest());

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task EventRangeRequestSchema_Validate_RejectsZeroPage()
	{
		var result = EventRangeRequestSchema.Validate(new EventRangeRequest(Page: 0));

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors).Contains(e => e.Path.Contains("Page"));
	}

	[Test]
	public async Task EventRangeRequestSchema_Validate_RejectsNegativePageSize()
	{
		var result = EventRangeRequestSchema.Validate(new EventRangeRequest(PageSize: -1));

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors).Contains(e => e.Path.Contains("PageSize"));
	}

	[Test]
	public async Task EventRangeRequestSchema_Validate_AcceptsNullableVersions()
	{
		var result = EventRangeRequestSchema.Validate(new EventRangeRequest(VersionFrom: 1, VersionTo: 100));

		await Assert.That(result.IsSuccess).IsTrue();
	}

	[Test]
	public async Task EventRangeRequestSchema_Validate_RejectsInvalidSortExpression()
	{
		var result = EventRangeRequestSchema.Validate(new EventRangeRequest(Sort: "Version; DROP TABLE Events;"));

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors).Contains(e => e.Path.Contains("Sort"));
	}

	[Test]
	public async Task AggregateSearchRequestSchema_Validate_RejectsOversizedAggregateType()
	{
		var result = AggregateSearchRequestSchema.Validate(
			new AggregateSearchRequest(new string('a', 257), null, null, null, null, null)
		);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.Errors).Contains(e => e.Path.Contains("AggregateType"));
	}

	[Test]
	public async Task AggregateSearchRequestSchema_Validate_RejectsZeroPage()
	{
		var result = AggregateSearchRequestSchema.Validate(
			new AggregateSearchRequest(null, null, null, null, null, null, Page: 0)
		);

		await Assert.That(result.IsSuccess).IsFalse();
	}

	[Test]
	public async Task AggregateSearchRequestSchema_Validate_AcceptsValidRequest()
	{
		var result = AggregateSearchRequestSchema.Validate(
			new AggregateSearchRequest("OrderAggregate", "order-1", null, null, null, null, 1, 25)
		);

		await Assert.That(result.IsSuccess).IsTrue();
	}
}
