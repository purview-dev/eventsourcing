using Purview.EventSourcing.Samples;
using Purview.EventSourcing.Samples.Fixtures;

namespace Purview.EventSourcing.Samples.Web.Infrastructure;

[NotInParallel("SamplesAppHost")]
[ClassDataSource<AppHostFixture>(Shared = SharedType.PerTestSession)]
public sealed class AzureVariantStartupUsageTests(AppHostFixture fixture)
{
	[Test]
	public Task AzureSqlVariant_StartsAndServesDashboard(CancellationToken cancellationToken) =>
		AssertVariantDashboardAsync(Platform.AzureSqlWebApp, cancellationToken);

	[Test]
	public Task AzurePostgresVariant_StartsAndServesDashboard(CancellationToken cancellationToken) =>
		AssertVariantDashboardAsync(Platform.AzurePostgresWebApp, cancellationToken);

	[Test]
	public Task AzureMongoVariant_StartsAndServesDashboard(CancellationToken cancellationToken) =>
		AssertVariantDashboardAsync(Platform.AzureMongoDbWebApp, cancellationToken);

	async Task AssertVariantDashboardAsync(string resourceName, CancellationToken cancellationToken)
	{
		using var client = fixture.CreateWebClient(resourceName, followRedirects: true);

		var response = await client.GetAsync("/", cancellationToken);
		await Assert.That(response.IsSuccessStatusCode).IsTrue();

		var html = await response.Content.ReadAsStringAsync(cancellationToken);
		await Assert.That(html).Contains("Customer Experience");
		await Assert.That(html).Contains("Back Office");
	}
}
