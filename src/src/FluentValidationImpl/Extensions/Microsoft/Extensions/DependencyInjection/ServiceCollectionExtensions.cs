using System.ComponentModel;
using FluentValidation;
using Purview.EventSourcing.FluentValidation.Services;
using Purview.EventSourcing.Services;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers a <see cref="FluentValidationAggregateValidator{TAggregate}"/> adapter
	/// for the specified aggregate type, wrapping the registered
	/// <see cref="IValidator{TAggregate}"/>.
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <typeparam name="TValidator">The FluentValidation validator implementation.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddFluentValidationAdapter<TAggregate, TValidator>(
		this IServiceCollection services
	)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
		where TValidator : class, IValidator<TAggregate>
	{
		services.AddSingleton<IValidator<TAggregate>, TValidator>();
		services.AddSingleton<IAggregateValidator<TAggregate>, FluentValidationAggregateValidator<TAggregate>>();
		return services;
	}

	/// <summary>
	/// Registers a <see cref="FluentValidationAggregateValidator{TAggregate}"/> adapter
	/// using a factory that resolves <see cref="IValidator{TAggregate}"/> from the container.
	/// Use this when validators are already registered (e.g. via <c>AddValidatorsFromAssembly</c>).
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddFluentValidationAdapter<TAggregate>(this IServiceCollection services)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
	{
		services.AddSingleton<IAggregateValidator<TAggregate>>(sp =>
		{
			var validator = sp.GetService<IValidator<TAggregate>>();
			return validator is null ? null! : new FluentValidationAggregateValidator<TAggregate>(validator);
		});
		return services;
	}
}
