using Microsoft.AspNetCore.Authorization;

namespace Purview.EventSourcing.Admin.Security;

public sealed class AggregateTypeAccessRequirement : IAuthorizationRequirement { }
