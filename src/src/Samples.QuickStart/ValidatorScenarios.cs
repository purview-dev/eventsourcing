using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Samples.Domain;

namespace Purview.EventSourcing.Samples.QuickStart;

sealed class ValidatorScenarios() : IAsyncDisposable
{
	readonly List<AsyncServiceScope> _scopes = [];

	IServiceProvider BuildServices(Action<IServiceCollection>? action = null)
	{
		ServiceCollection services = [];

		services.AddLogging();

		services.AddInMemoryEventStore().AddInMemorySnapshotEventStore();

		action?.Invoke(services);

		var scope = services.BuildServiceProvider().CreateAsyncScope();
		_scopes.Add(scope);

		return scope.ServiceProvider;
	}

	static CustomerAggregate CreateAggregate(int numberOfChars = 200)
	{
		CustomerAggregate agg = new();

		return agg.ChangePhoneNumber(new string('1', numberOfChars));
	}

	public async Task ValidateAsync(CancellationToken cancellationToken)
	{
		await ValidateBuiltInAsync(cancellationToken);
		await ValidateFluentValidatorAsync(cancellationToken);
		await ValidateZodSharpAsync(cancellationToken);
	}

	async Task ValidateBuiltInAsync(CancellationToken cancellationToken)
	{
		var sp = BuildServices();
		var store = sp.GetRequiredService<IQueryableEventStore>();

		var agg = CreateAggregate();

		var result = await store.SaveAsync(agg, cancellationToken);

		DisplaySummary("BuiltIn", result);
	}

	async Task ValidateFluentValidatorAsync(CancellationToken cancellationToken)
	{
		var sp = BuildServices(services => services.AddDomainFluentValidators());
		var store = sp.GetRequiredService<IQueryableEventStore>();

		var agg = CreateAggregate();

		var result = await store.SaveAsync(agg, cancellationToken);

		DisplaySummary("FluentValidation", result);
	}

	async Task ValidateZodSharpAsync(CancellationToken cancellationToken)
	{
		var sp = BuildServices(services => services.AddDomainZodValidators());
		var store = sp.GetRequiredService<IQueryableEventStore>();

		var agg = CreateAggregate();

		var result = await store.SaveAsync(agg, cancellationToken);

		DisplaySummary("ZodSharp", result);
	}

	static void DisplaySummary(string type, SaveResult<CustomerAggregate> result)
	{
		Console.WriteLine($"Validated '{type}':");
		if (result.IsValid)
			Console.WriteLine("  FAILED: Did not validate correctly!!");
		else
		{
			if (result.FailureCount != 1)
				Console.WriteLine(
					$"  FAILED: Should be exactly 1 error, but found {result.FailureCount}"
				);
			else if (
				result.ValidationResult.Failures[0].PropertyName
				!= nameof(CustomerAggregate.PhoneNumber)
			)
				Console.WriteLine(
					$"   FAILED: Error should have validatedd '{nameof(CustomerAggregate.PhoneNumber)}', but found '{result.ValidationResult.Failures[0].PropertyName}'"
				);
			else
				Console.WriteLine(
					$"  SUCCESS: Found {result.ValidationResult.Failures[0].ErrorMessage}"
				);
		}

		Console.WriteLine();
	}

	public async ValueTask DisposeAsync()
	{
		foreach (var scope in _scopes)
			await scope.DisposeAsync();
	}
}
