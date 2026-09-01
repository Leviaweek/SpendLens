using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SpendLensApi;
using SpendLensDatabase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>();

ArgumentNullException.ThrowIfNull(jwtOptions);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RefreshTokenAuthentication.Scheme, policy =>
    {
        policy.AddAuthenticationSchemes(
            RefreshTokenAuthentication.Scheme);

        policy.RequireAuthenticatedUser();
    });
});
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
    })
    .AddScheme<
        AuthenticationSchemeOptions,
        RefreshTokenAuthenticationHandler>(
        RefreshTokenAuthentication.Scheme,
        _ => { });

builder.Services.AddDbContext<SpendLensDbContext>(h =>
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

app.MapAuthEndpoints();

app.UseAuthentication();
app.UseAuthorization();

//app.UseHttpsRedirection();

await app.RunAsync();