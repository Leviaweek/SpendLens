using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SpendLensDatabase;

namespace SpendLensApi.Auth.RefreshTokens;

public sealed class RefreshTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    RefreshTokenService refreshTokenService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenService.CookieName, out var rawToken))
            return AuthenticateResult.NoResult();

        if (string.IsNullOrWhiteSpace(rawToken))
            return AuthenticateResult.Fail("Bad credentials");

        try
        {
            var (identityId, verifyId) =
                RefreshTokenGenerator.Split(rawToken);

            if (!await refreshTokenService.ValidateAsync(identityId, verifyId, Context.RequestAborted))
                return AuthenticateResult.NoResult();
            
            var claims = new[]
            {
                new Claim(
                    RefreshTokenAuthenticationContext.IdentityIdClaim,
                    identityId.ToString("N")),
            };

            var identity = new ClaimsIdentity(
                claims,
                Scheme.Name);

            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(
                principal,
                Scheme.Name);
            
            return AuthenticateResult.Success(ticket);
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Bad credentials");
        }
        catch (ArgumentException)
        {
            return AuthenticateResult.Fail("Bad credentials");
        }
    }
}