using Microsoft.AspNetCore.Authorization;
using Purview.EventSourcing.Admin.Abstractions.Models;

namespace Purview.EventSourcing.Admin.Security.Requirements;

public sealed class AdminFeatureRequirement(AdminFeature feature) : IAuthorizationRequirement
{
	public AdminFeature Feature => feature;
}
