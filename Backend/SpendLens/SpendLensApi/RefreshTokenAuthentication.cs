using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SpendLensDatabase.Models.Auth;

namespace SpendLensApi;

public static class RefreshTokenAuthentication
{
    public const string Scheme = "RefreshToken";

    public const string IdentityIdClaim = "identity_id";
    public const string VerifyIdClaim = "verify_id";
}

public sealed class RefreshTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Task.Yield();
        
        if (!Request.Cookies.TryGetValue(JwtService.RefreshTokenCookieName, out var rawToken))
            return AuthenticateResult.NoResult();

        if (string.IsNullOrWhiteSpace(rawToken))
            return AuthenticateResult.Fail("Bad credentials");

        try
        {
            var (identityId, verifyId) =
                RefreshTokenGenerator.Split(rawToken);

            var claims = new[]
            {
                new Claim(
                    RefreshTokenAuthentication.IdentityIdClaim,
                    identityId.ToString("N")),

                new Claim(
                    RefreshTokenAuthentication.VerifyIdClaim,
                    verifyId.ToString("N"))
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