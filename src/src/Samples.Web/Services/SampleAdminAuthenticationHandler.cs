using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Samples.Web.Services;

sealed class SampleAdminAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "SampleAdmin";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.NameIdentifier, "sample-admin"), new Claim(ClaimTypes.Name, "Sample Admin")],
			SchemeName
		);

		var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
