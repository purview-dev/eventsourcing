using Microsoft.AspNetCore.Authorization;

namespace Purview.EventSourcing.Admin.Security.Requirements;

public sealed class AggregateTypeAccessRequirement : IAuthorizationRequirement { }
