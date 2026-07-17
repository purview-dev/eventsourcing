using System.ComponentModel;
using Purview.EventSourcing.Services;
using Purview.EventSourcing.ZodSharp.Services;
using ZodSharp.Core;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers a <see cref="ZodSharpAggregateValidator{TAggregate}"/> adapter
	/// for the specified aggregate type, wrapping the registered
	/// <see cref="IValidator{TAggregate}"/>.
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <typeparam name="TValidator">The ZodSharp schema validator implementation.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddZodSharpAdapter<TAggregate, TValidator>(this IServiceCollection services)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
		where TValidator : class, IZodSchemaValidator<TAggregate>
	{
		services.AddSingleton<IZodSchemaValidator<TAggregate>, TValidator>();
		services.AddSingleton<IAggregateValidator<TAggregate>, ZodSharpAggregateValidator<TAggregate>>();
		return services;
	}

	/// <summary>
	/// Registers a <see cref="ZodSharpAggregateValidator{TAggregate}"/> adapter
	/// using a factory that resolves <see cref="ISchemaValidator{TAggregate}"/> from the container.
	/// Use this when validators are already registered (e.g. via <c>AddValidatorsFromAssembly</c>).
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddZodSharpAdapter<TAggregate>(this IServiceCollection services)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
	{
		services.AddSingleton<IAggregateValidator<TAggregate>>(sp =>
		{
			var validator = sp.GetService<IZodSchemaValidator<TAggregate>>();
			return validator is null ? null! : new ZodSharpAggregateValidator<TAggregate>(validator);
		});
		return services;
	}
}
