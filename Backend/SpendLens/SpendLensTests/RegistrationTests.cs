using Microsoft.Extensions.Logging.Abstractions;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Auth.Registration;
using SpendLensDatabase;

namespace SpendLensTests;

[Collection("Integration")]
public sealed class RegistrationTests: IAsyncDisposable
{
    private readonly PostgresFixture _postgresFixture;
    private readonly SpendLensDbContext _dbContext;
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly RegistrationService _registerService;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public RegistrationTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        _dbContext = postgresFixture.CreateContext();
        
        var refreshTokenService = new RefreshTokenService(_dbContext,
            NullLogger<RefreshTokenService>.Instance);
        
        _registerService = new RegistrationService(_dbContext,
            refreshTokenService,
            NullLogger<RegistrationService>.Instance);
    }

    [Fact]
    public async Task Registration_WithValidCredentials_ReturnSuccess()
    {
        var user = UserModelFactory.CreateUserModel();
        var organization = OrganizationModelFactory.CreateOrganizationModel();
        var registration = new RegistrationModel(user, organization);

        var result = await _registerService.RegisterAsync(registration, RefreshTokenLifetime, _cancellationToken);
        
        var success = Assert.IsType<RegisterResult.Success>(result);

        var dbUser = _dbContext.Users.First();
        
        Assert.Equal(success.User.Id, dbUser.Id);
        Assert.Equal(success.User.Email, dbUser.Email);
        Assert.Equal(success.User.CreatedAt, dbUser.CreatedAt);
    }

    [Fact]
    public async Task Registration_ExistingEmail_ReturnEmailTaken()
    {
        var (user, password) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: TestContext.Current.CancellationToken);
        
        var userModel = UserModelFactory.CreateUserModel(user.Email, password);
        var organization = OrganizationModelFactory.CreateOrganizationModel();
        var registration = new RegistrationModel(userModel, organization);
        
        var result = await _registerService.RegisterAsync(registration, RefreshTokenLifetime, _cancellationToken);
        Assert.IsType<RegisterResult.EmailTaken>(result);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresFixture.ResetAsync();
    }
}