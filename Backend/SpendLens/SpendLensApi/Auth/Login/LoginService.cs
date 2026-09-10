using Microsoft.EntityFrameworkCore;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Contracts.Users;
using SpendLensDatabase;

namespace SpendLensApi.Auth.Login;

public sealed class LoginService(
    SpendLensDbContext context,
    RefreshTokenService refreshTokenService,
    ILogger<LoginService> logger)
{
    private const string DummyPassword = "dummy1";
    private const string DummyHash = "$2a$11$Z4gsv8S3WNaIP/uefFyxAOu4ghKbfz8K9m5IwTuS74NejWQ5n7KRe";

    public async Task<LoginResult> LoginAsync(
        UserModel model,
        TimeSpan refreshTokenLifetime,
        CancellationToken cancellationToken)
    {
        var email = model.Email.ToLowerInvariant();

        logger.LogInformation(
            "Login attempt for {Email}",
            email);

        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Email == email,
            cancellationToken);

        if (user is null)
        {
            logger.LogWarning(
                "Login failed: user with email {Email} was not found",
                email);

            BCrypt.Net.BCrypt.Verify(DummyPassword, DummyHash);

            return new LoginResult.Unauthorized();
        }

        logger.LogDebug(
            "User {UserId} found for login",
            user.Id);

        var verifyResult = BCrypt.Net.BCrypt.Verify(
            model.Password,
            user.PasswordHash);

        if (!verifyResult)
        {
            logger.LogWarning(
                "Login failed: invalid password for user {UserId}",
                user.Id);

            return new LoginResult.Unauthorized();
        }

        logger.LogDebug(
            "Password verified for user {UserId}",
            user.Id);

        var rawToken = refreshTokenService.AddRefreshToken(
            refreshTokenLifetime,
            user);

        logger.LogDebug(
            "Refresh token created for user {UserId}",
            user.Id);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Login successful for user {UserId}",
            user.Id);

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.CreatedAt);

        return new LoginResult.Success(userDto, rawToken);
    }
}