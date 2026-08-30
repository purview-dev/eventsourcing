using Microsoft.AspNetCore.Authorization;

namespace Purview.EventSourcing.Admin.Security.Requirements;

/// <summary>
/// Authorization requirement that indicates the user must have general access to the aggregate being requested.
/// </summary>
/// <remarks>
/// <para>
/// The requirement is satisfied by <see cref="Handlers.AggregateTypeAccessHandler"/> when the user holds at
/// least one granted permission, while deny-by-default and per-aggregate-type checks are enforced by the
/// endpoint layer.
/// </para>
/// </remarks>
public sealed class AggregateTypeAccessRequirement : IAuthorizationRequirement { }
