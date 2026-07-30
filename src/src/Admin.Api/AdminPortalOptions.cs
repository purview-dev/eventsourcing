using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing.Admin.Api;

public sealed class AdminPortalOptions
{
	public const string Section = "AdminPortal";

	public bool Enabled { get; set; } = true;

	public string RoutePrefix { get; set; } = "/admin/api";

	public AdminFeatureOptions Features { get; set; } = new();

	public AdminPagingOptions Paging { get; set; } = new();

	public AdminProjectionOptions Projections { get; set; } = new();

	public void Validate()
	{
		if (string.IsNullOrWhiteSpace(RoutePrefix))
			throw new InvalidOperationException("AdminPortalOptions.RoutePrefix cannot be empty.");

		if (!RoutePrefix.StartsWith("/"))
			throw new InvalidOperationException("AdminPortalOptions.RoutePrefix must start with '/'.");

		Features.Validate();
		Paging.Validate();
		Projections.Validate();
	}
}

public sealed class AdminFeatureOptions
{
	public bool SearchAggregates { get; set; } = true;
	public bool ViewAggregate { get; set; } = true;
	public bool ViewEvents { get; set; } = true;
	public bool ProjectPointInTime { get; set; } = true;
	public bool ExportEvents { get; set; } = false;

	public void Validate()
	{
	}
}

public sealed class AdminPagingOptions
{
	public int DefaultPageSize { get; set; } = 50;
	public int MaxPageSize { get; set; } = 500;

	public void Validate()
	{
		if (DefaultPageSize < 1 || DefaultPageSize > MaxPageSize)
			throw new InvalidOperationException(
				"AdminPagingOptions.DefaultPageSize must be between 1 and MaxPageSize.");

		if (MaxPageSize < 1)
			throw new InvalidOperationException("AdminPagingOptions.MaxPageSize must be >= 1.");
	}
}

public sealed class AdminProjectionOptions
{
	public int MaxVersionsPerQuery { get; set; } = 10000;
	public TimeSpan MaxTimeRangePerQuery { get; set; } = TimeSpan.FromDays(365);

	public void Validate()
	{
		if (MaxVersionsPerQuery < 1)
			throw new InvalidOperationException("AdminProjectionOptions.MaxVersionsPerQuery must be >= 1.");

		if (MaxTimeRangePerQuery < TimeSpan.Zero)
			throw new InvalidOperationException("AdminProjectionOptions.MaxTimeRangePerQuery cannot be negative.");
	}
}
