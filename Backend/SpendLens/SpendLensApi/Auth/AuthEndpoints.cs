using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SpendLensApi.Auth.Login;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Auth.Registration;
using SpendLensApi.Contracts.Users;

namespace SpendLensApi.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<ValidationFilter<RegistrationModel>>();

        group.MapPost("/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<UserModel>>();
        
        group.MapPost("/refresh", RefreshAsync)
            .RequireAuthorization(RefreshTokenAuthenticationContext.Scheme);
            
    }

    private static async Task<Results<Created<UserDto>, Conflict, ProblemHttpResult>> RegisterAsync(
        [FromBody] RegistrationModel request, 
        [FromServices] AccessTokenService accessTokenService,
        [FromServices] RegistrationService db, 
        [FromServices] IOptions<JwtOptions> jwtOptions,
        HttpContext http,
        CancellationToken cancellationToken
    )
    {
        var jwt = jwtOptions.Value;
        
        var result = await db.CreateAuthModelsAsync(request, 
            TimeSpan.FromDays(jwt.RefreshTokenExpirationDays),
            cancellationToken);

        return result switch
        {
            RegisterResult.Success success => SuccessLogin(success, accessTokenService, http),
            RegisterResult.EmailTaken => TypedResults.Conflict(),
            _ => TypedResults.Problem()
        };
    }
    
    private static Created<UserDto> SuccessLogin(RegisterResult.Success success, AccessTokenService accessTokenService, HttpContext http)
    {
        AddTokens(success.User, success.RefreshToken, accessTokenService, http);

        return TypedResults.Created($"/users/{success.User.Id:N}", success.User);
    }

    private static void AddTokens(UserDto user, string rawToken,AccessTokenService accessTokenService, HttpContext http)
    {
        var token = accessTokenService.GenerateToken(user.Id.ToString("N"), user.Email);
        
        http.Response.Cookies.Append(AccessTokenService.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
        
        http.Response.Cookies.Append(RefreshTokenService.CookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
        });
    }

    private static async Task<Results<Ok<UserDto>, UnauthorizedHttpResult, ProblemHttpResult>> LoginAsync(
        [FromBody] UserModel request,
        [FromServices] AccessTokenService accessTokenService,
        [FromServices] LoginService db,
        [FromServices] IOptions<JwtOptions> jwtOptions,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var jwt = jwtOptions.Value;
        var loginResult = await db.LoginAsync(request,
            TimeSpan.FromDays(jwt.RefreshTokenExpirationDays),
            cancellationToken);

        return loginResult switch
        {
            LoginResult.Success success => SuccessLogin(success, accessTokenService, http),
            LoginResult.Unauthorized => TypedResults.Unauthorized(),
            _ => TypedResults.Problem()
        };
    }

    private static Ok<UserDto> SuccessLogin(LoginResult.Success success,AccessTokenService accessTokenService, HttpContext http)
    {
        AddTokens(success.User, success.RefreshToken, accessTokenService, http);
        return TypedResults.Ok(success.User);
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, ProblemHttpResult>> RefreshAsync(
        [FromServices] RefreshTokenService service,
        [FromServices] IOptions<JwtOptions> jwtOptions,
        [FromServices] AccessTokenService accessTokenService,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var idClaims = http.User.FindFirstValue(RefreshTokenAuthenticationContext.IdentityIdClaim);
        
        ArgumentNullException.ThrowIfNull(idClaims);
        
        var id = Guid.Parse(idClaims);

        var jwt = jwtOptions.Value;
        
        var rotationResult = await service.RotateAsync(id,
            TimeSpan.FromDays(jwt.RefreshTokenExpirationDays),
            cancellationToken);

        return rotationResult switch
        {
            RefreshResult.Success success => SuccessRefresh(success,accessTokenService, http),
            RefreshResult.ReuseOrNotFound => TypedResults.Unauthorized(),
            _ => TypedResults.Problem()
        };
    }

    private static Ok SuccessRefresh(RefreshResult.Success success, AccessTokenService accessTokenService,HttpContext http)
    {
        AddTokens(success.User, success.RawToken, accessTokenService, http);
        return TypedResults.Ok();
    }
}