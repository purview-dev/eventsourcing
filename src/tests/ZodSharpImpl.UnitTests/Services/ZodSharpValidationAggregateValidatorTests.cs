using Purview.EventSourcing.Aggregates;
using ZodSharp.Core;

namespace Purview.EventSourcing.ZodSharp.Services;

public sealed class ZodSharpAggregateValidatorTests
{
	[Test]
	public async Task ValidateAsync_UsesAsyncRules(CancellationToken cancellationToken)
	{
		var asyncRuleInvoked = false;
		var aggregate = new TestAggregate { Name = "invalid" };
		var validator = IZodSchemaValidator<TestAggregate>.Mock();

		var adapter = new ZodSharpAggregateValidator<TestAggregate>(validator);

		var result = await adapter.ValidateAsync(aggregate, cancellationToken);

		await Assert.That(asyncRuleInvoked).IsTrue();
		await Assert.That(result.IsValid).IsTrue();
	}

	[Test]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1849:Call async methods when in an async method"
	)]
	public async Task Validate_WhenFluentValidationFails_MapsToCoreValidationResult()
	{
		var aggregate = new TestAggregate { Name = "" };
		var validator = IZodSchemaValidator<TestAggregate>.Mock();

		var adapter = new ZodSharpAggregateValidator<TestAggregate>(validator);

		var result = adapter.Validate(aggregate);

		await Assert.That(result.IsValid).IsFalse();
		await Assert.That(result.Errors).Count().IsEqualTo(1);
		await Assert.That(result.Errors[0].PropertyName).IsEqualTo("Name");
	}

	sealed class TestAggregate : AggregateBase
	{
		public string Name { get; set; } = string.Empty;

		protected override void RegisterEvents() { }
	}
}
