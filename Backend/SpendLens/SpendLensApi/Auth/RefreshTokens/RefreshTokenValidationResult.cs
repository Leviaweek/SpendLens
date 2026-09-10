namespace SpendLensApi.Auth.RefreshTokens;

public abstract record RefreshTokenValidationResult
{
    public sealed record Success : RefreshTokenValidationResult;
    public sealed record NotFound : RefreshTokenValidationResult;
    public sealed record Invalid : RefreshTokenValidationResult;
    public sealed record ReuseDetected : RefreshTokenValidationResult;
}