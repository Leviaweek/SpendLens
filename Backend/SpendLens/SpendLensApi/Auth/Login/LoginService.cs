using Microsoft.EntityFrameworkCore;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Auth.Registration;
using SpendLensDatabase;
using SpendLensDatabase.Models.AuthData.Users;

namespace SpendLensApi.Auth.Login;

public sealed class LoginService(SpendLensDbContext context, RefreshTokenService refreshTokenService)
{
    private const string DummyPassword = "dummy1";
    private const string DummyHash = "$2a$11$Z4gsv8S3WNaIP/uefFyxAOu4ghKbfz8K9m5IwTuS74NejWQ5n7KRe";

    public async Task<LoginResult> LoginAsync(UserCreationModel creationModel,
        TimeSpan refreshTokenLifetime,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == creationModel.Email,
            cancellationToken: cancellationToken);

        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(DummyPassword, DummyHash);
            return new LoginResult.Unauthorized();
        }
        
        var verifyResult = BCrypt.Net.BCrypt.Verify(creationModel.Password, user.PasswordHash);

        if (!verifyResult)
            return new LoginResult.Unauthorized();

        var rawToken = refreshTokenService.AddRefreshToken(refreshTokenLifetime, user);
        
        await context.SaveChangesAsync(cancellationToken);
        
        var userDto = new UserDto(user.Id, user.Email, user.CreatedAt);

        return new LoginResult.Success(userDto, rawToken);
    }
}