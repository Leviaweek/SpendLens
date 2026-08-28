namespace SpendLensApi;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public required string Secret { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int AccessTokenExpirationMinutes { get; init; }
    public required int RefreshTokenExpirationDays { get; init; }
}