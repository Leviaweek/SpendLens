using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SpendLensDatabase;
using SpendLensDatabase.Models.Auth;
using SpendLensDatabase.Models.Auth.Users;

namespace SpendLensApi;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        // group.MapPost("/refresh", RefreshAsync);
    }

    private static async Task<Results<Created<UserDto>, Conflict, ProblemHttpResult>> RegisterAsync(
        [FromBody] RegistrationModel request, 
        [FromServices] JwtService jwtService,
        [FromServices] SpendLensDb db, 
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
            RegisterResult.Success success => SuccessLogin(success, jwtService, http),
            RegisterResult.EmailTaken => TypedResults.Conflict(),
            _ => TypedResults.Problem()
        };
    }
    
    private static Created<UserDto> SuccessLogin(RegisterResult.Success success, JwtService jwtService, HttpContext http)
    {
        AddTokens(success.User, success.RefreshToken, jwtService, http);

        return TypedResults.Created($"/users/{success.User.Id:N}", success.User);
    }

    private static void AddTokens(UserDto user, string rawToken,JwtService jwtService, HttpContext http)
    {
        var token = jwtService.GenerateToken(user.Id.ToString("N"), user.Email);
        
        http.Response.Cookies.Append(JwtService.AccessCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
        
        http.Response.Cookies.Append(JwtService.RefreshTokenCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
        });
    }

    private static async Task<Results<Ok<UserDto>, Conflict, ProblemHttpResult>> LoginAsync(
        [FromBody] UserCreationModel request,
        [FromServices] JwtService jwtService,
        [FromServices] SpendLensDb db,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var loginResult = await db.LoginAsync(request, TimeSpan.FromDays(30), cancellationToken);

        return loginResult switch
        {
            LoginResult.Success success => SuccessLogin(success, jwtService, http),
            LoginResult.Failure => TypedResults.Conflict(),
            _ => TypedResults.Problem()
        };
    }

    private static Ok<UserDto> SuccessLogin(LoginResult.Success success,JwtService jwtService, HttpContext http)
    {
        AddTokens(success.User, success.RefreshToken, jwtService, http);
        return TypedResults.Ok(success.User);
    }
}