using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace SpendLensApi;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    [Required]
    [MinLength(32)]   
    public required string Secret { get; init; }
    
    [Required]
    [MinLength(1)]
    public required string Issuer { get; init; }
    
    [Required]
    [MinLength(1)]
    public required string Audience { get; init; }
    
    [Range(1, int.MaxValue)]
    public required int AccessTokenExpirationMinutes { get; init; }
    
    [Range(1, int.MaxValue)]
    public required int RefreshTokenExpirationDays { get; init; }
}