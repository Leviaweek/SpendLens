using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

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
        if (!Request.Cookies.TryGetValue(
                RefreshTokenService.CookieName,
                out var rawToken))
        {
            return AuthenticateResult.NoResult();
        }

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            Logger.LogDebug(
                "Refresh token authentication failed: empty token");

            return AuthenticateResult.Fail("Bad credentials");
        }

        try
        {
            var (identityId, verifyId) =
                RefreshTokenGenerator.Split(rawToken);

            var result = await refreshTokenService.ValidateAsync(
                identityId,
                verifyId,
                Context.RequestAborted);

            if (result is not RefreshTokenValidationResult.Success)
                return AuthenticateResult.Fail("Bad credentials");

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

            Logger.LogDebug(
                "Refresh token authentication succeeded for token {RefreshTokenId}",
                identityId);

            return AuthenticateResult.Success(ticket);
        }
        catch (FormatException)
        {
            Logger.LogDebug(
                "Refresh token authentication failed: malformed token");

            return AuthenticateResult.Fail("Bad credentials");
        }
        catch (ArgumentException)
        {
            Logger.LogDebug(
                "Refresh token authentication failed: malformed token");

            return AuthenticateResult.Fail("Bad credentials");
        }
    }
}