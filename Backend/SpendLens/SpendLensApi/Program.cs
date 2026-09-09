using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SpendLensApi;
using SpendLensApi.Auth;
using SpendLensApi.Auth.Login;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Auth.Registration;
using SpendLensDatabase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(RefreshTokenAuthenticationContext.Scheme, policy =>
    {
        policy.AddAuthenticationSchemes(
            RefreshTokenAuthenticationContext.Scheme);

        policy.RequireAuthenticatedUser();
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer()
    .AddScheme<
        AuthenticationSchemeOptions,
        RefreshTokenAuthenticationHandler>(
        RefreshTokenAuthenticationContext.Scheme,
        _ => { });

builder.Services.AddOptions<JwtBearerOptions>()
    .Configure<IOptions<JwtOptions>>((jwtBearer, jwt) =>
    {
        jwtBearer.MapInboundClaims = false;

        jwtBearer.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[
                    AccessTokenService.CookieName];

                return Task.CompletedTask;
            }
        };

        jwtBearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Value.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Value.Audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.Value.Secret))
        };
    });

builder.Services.AddDbContext<SpendLensDbContext>(h =>
{
    var connectionString = builder.Configuration.GetConnectionString(SpendLensDbContext.OptionName);
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    var dataSource = dataSourceBuilder.Build();
    h.UseNpgsql(dataSource);
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<RefreshTokenService>();

builder.Services.AddSingleton<AccessTokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

//app.UseHttpsRedirection();

await app.RunAsync();