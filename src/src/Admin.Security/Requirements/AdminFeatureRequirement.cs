using Microsoft.AspNetCore.Authorization;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.Security;

public sealed class AdminFeatureRequirement(AdminFeature feature) : IAuthorizationRequirement
{
	public AdminFeature Feature => feature;
}
