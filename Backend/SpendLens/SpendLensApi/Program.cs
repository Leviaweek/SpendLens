using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SpendLensApi;
using SpendLensDatabase;
using SpendLensDatabase.Models.Auth;
using SpendLensDatabase.Models.Auth.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>();

ArgumentNullException.ThrowIfNull(jwtOptions);

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            
            ValidateLifetime = true,
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =  new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        };
    });

builder.Services.AddDbContextFactory<SpendLensDbContext>(h =>
{
    var connectionString = builder.Configuration.GetConnectionString(SpendLensDbContext.OptionName);
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    var dataSource = dataSourceBuilder.Build();
    h.UseNpgsql(dataSource);
});

builder.Services.AddScoped<SpendLensDb>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<JwtService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/auth/register", async Task<Results<Created<UserDto>, Conflict, ProblemHttpResult>> 
    ([FromBody] RegistrationModel request, 
        [FromServices] JwtService jwtService,
        [FromServices] SpendLensDb db, 
        HttpContext http,
        CancellationToken cancellationToken) =>
{
    var result = await db.CreateAuthModelsAsync(request, cancellationToken);

    return result switch
    {
        RegisterResult.Success success => Success(success, jwtService, http),
        RegisterResult.EmailTaken => TypedResults.Conflict(),
        _ => TypedResults.Problem()
    };

    static Created<UserDto> Success(RegisterResult.Success success, JwtService jwtService, HttpContext http)
    {
        var token = jwtService.GenerateToken(success.User.Id.ToString("N"), success.User.Email);
        
        http.Response.Cookies.Append(JwtService.AccessCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
        
        http.Response.Cookies.Append(JwtService.RefreshTokenCookieName, success.RawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
        });
        
        return TypedResults.Created($"/users/{success.User.Id:N}", success.User);
    }
});

app.UseAuthentication();
app.UseAuthorization();

//app.UseHttpsRedirection();

await app.RunAsync();