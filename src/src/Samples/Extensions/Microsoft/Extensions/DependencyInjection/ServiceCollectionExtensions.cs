using System.ComponentModel;
using Purview.EventSourcing.Samples.Domain;
using Purview.EventSourcing.Samples.Domain.Validators;
using Purview.EventSourcing.Samples.Services;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddDomainServices()
		{
			services.AddScoped<ISeedDataService, SeedDataService>();
			services.AddScoped<IOrderFulfilmentService, OrderFulfilmentService>();
			services.AddScoped<IStockTransferService, StockTransferService>();
			services.AddScoped<ICartCheckoutService, CartCheckoutService>();

			return services;
		}

		public IServiceCollection AddDomainZodValidators()
		{
			services.AddZodSharpAdapter<CustomerAggregate, CustomerAggregateSchemaValidator>();

			return services;
		}

		public IServiceCollection AddDomainFluentValidators()
		{
			services.AddFluentValidationAdapter<CustomerAggregate, CustomerAggregateValidator>();

			return services;
		}
	}
}
