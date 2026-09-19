using Microsoft.Extensions.Logging.Abstractions;
using SpendLensApi.Auth.Login;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Auth.Registration;
using SpendLensApi.Contracts.Users;
using SpendLensDatabase;

namespace SpendLensTests;

[Collection("Integration")]
public sealed class LoginTests: IAsyncDisposable
{
    private readonly PostgresFixture _postgresFixture;
    private readonly SpendLensDbContext _dbContext;
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly LoginService _loginService;
    private readonly RegistrationService _registerService;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public LoginTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        _dbContext = postgresFixture.CreateContext();
        
        var refreshTokenService = new RefreshTokenService(_dbContext,
            NullLogger<RefreshTokenService>.Instance);
        
        _registerService = new RegistrationService(_dbContext,
            refreshTokenService,
            NullLogger<RegistrationService>.Instance);
        
        _loginService = new LoginService(_dbContext,
            refreshTokenService,
            NullLogger<LoginService>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresFixture.ResetAsync();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        var (user, password) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);
        
        var userModel = new UserModel(user.Email, password);
        
        var result = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        var success = Assert.IsType<LoginResult.Success>(result);
        
        Assert.Equal(user.Email, success.User.Email);
        Assert.Equal(user.Id, success.User.Id);
    }

    [Fact]
    public async Task RegisterAndLogin_WithValidCredentials_ReturnsSuccess()
    {
        var userModel = UserModelFactory.CreateUserModel();
        var organizationModel = OrganizationModelFactory.CreateOrganizationModel();
        var registrationModel = new RegistrationModel(userModel, organizationModel);
        
        var registerResult = await _registerService.RegisterAsync(registrationModel, RefreshTokenLifetime, _cancellationToken);
        
        var successRegister = Assert.IsType<RegisterResult.Success>(registerResult);
        
        var loginResult = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        var successLogin  = Assert.IsType<LoginResult.Success>(loginResult);
        
        Assert.Equal(successRegister.User.Email, successLogin.User.Email);
        Assert.Equal(successRegister.User.Id, successLogin.User.Id);
    }
    
    [Fact]
    public async Task Login_WithDifferentEmailCase_ReturnsSuccess()
    {
        var userModel = UserModelFactory.CreateUserModel("TestCase@example.com");
        var organizationModel = OrganizationModelFactory.CreateOrganizationModel();
        var registrationModel = new RegistrationModel(userModel, organizationModel);
        
        var registerResult = await _registerService.RegisterAsync(registrationModel, RefreshTokenLifetime, _cancellationToken);
        
        var successRegister = Assert.IsType<RegisterResult.Success>(registerResult);
        
        Assert.Equal(successRegister.User.Email.ToLowerInvariant(), successRegister.User.Email);
        
        var newUserModel = userModel with { Email = successRegister.User.Email.ToUpperInvariant() };
        
        var loginResult = await _loginService.LoginAsync(newUserModel, RefreshTokenLifetime, _cancellationToken);
        
        var successLogin  = Assert.IsType<LoginResult.Success>(loginResult);
        
        Assert.Equal(successRegister.User.Email, successLogin.User.Email);
        Assert.Equal(successRegister.User.Id, successLogin.User.Id);
    }
    
    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnUnauthorized()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);
        
        var userModel = new UserModel(user.Email, "incorrectpassword");
        
        var result = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        Assert.IsType<LoginResult.Unauthorized>(result);
    }
    
    [Fact]
    public async Task Login_WithIncorrectEmail_ReturnUnauthorized()
    {
        var (_, password) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);
        
        var userModel = new UserModel("incorrect@example.com", password);
        
        var result = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        Assert.IsType<LoginResult.Unauthorized>(result);
    }
}