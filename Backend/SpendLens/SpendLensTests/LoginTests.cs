using Microsoft.Extensions.Logging.Abstractions;
using SpendLensApi.Auth.Login;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Auth.Registration;
using SpendLensApi.Contracts.Organizations;
using SpendLensApi.Contracts.Users;
using SpendLensDatabase;
using SpendLensDatabase.Models.Entities;

namespace SpendLensTests;

[Collection("Integration")]
public sealed class LoginTests: IAsyncLifetime
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
        
        _registerService = new RegistrationService(_dbContext, refreshTokenService, NullLogger<RegistrationService>.Instance);
        
        _loginService = new LoginService(_dbContext,
            refreshTokenService,
            NullLogger<LoginService>.Instance);
    }
    
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _postgresFixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        var (user, password) = UserFactory.CreateUser();

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(_cancellationToken);
        
        var userModel = new UserModel(user.Email, password);
        
        var result = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        var success = Assert.IsType<LoginResult.Success>(result);
        
        Assert.Equal(user.Email, success.User.Email);
        Assert.Equal(user.Id, success.User.Id);
    }

    [Fact]
    public async Task RegisterAndLogin_WithValidCredentials_ReturnsSuccess()
    {
        var (user, password) = UserFactory.CreateUser();
        var organization = OrganizationFactory.CreateOrganization();
        
        var userModel = new UserModel(user.Email, password);
        var organizationModel = new OrganizationModel(organization.Name);
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
        var (user, password) = UserFactory.CreateUser(email: "TestCase@example.com");
        var organization = OrganizationFactory.CreateOrganization();
        
        var userModel = new UserModel(user.Email, password);
        var organizationModel = new OrganizationModel(organization.Name);
        var registrationModel = new RegistrationModel(userModel, organizationModel);
        
        var registerResult = await _registerService.RegisterAsync(registrationModel, RefreshTokenLifetime, _cancellationToken);
        
        var successRegister = Assert.IsType<RegisterResult.Success>(registerResult);
        
        Assert.Equal(successRegister.User.Email, user.Email.ToLowerInvariant());
        
        var loginResult = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        var successLogin  = Assert.IsType<LoginResult.Success>(loginResult);
        
        Assert.Equal(successRegister.User.Email, successLogin.User.Email);
        Assert.Equal(successRegister.User.Id, successLogin.User.Id);
    }
    
    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnUnauthorized()
    {
        var (user, password) = UserFactory.CreateUser();
        
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(_cancellationToken);
        
        var userModel = new UserModel(user.Email, "Pasdasdasdas");
        
        var result = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        Assert.IsType<LoginResult.Unauthorized>(result);
    }
    
    [Fact]
    public async Task Login_WithIncorrectEmail_ReturnUnauthorized()
    {
        var (user, password) = UserFactory.CreateUser();
        
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(_cancellationToken);
        
        var userModel = new UserModel("incorrect@example.com", password);
        
        var result = await _loginService.LoginAsync(userModel, RefreshTokenLifetime, _cancellationToken);
        
        Assert.IsType<LoginResult.Unauthorized>(result);
    }
}

public static class UserFactory
{
    private const string DefaultPassword = "P@ssw0rd123!";
    private const int TestWorkFactor = 4;

    public static (User User, string Password) CreateUser(
        string? email = null,
        string password = DefaultPassword,
        DateTime? createdAt = null)
    {
        var id = Guid.CreateVersion7();
        
        var user = new User
        {
            Id = id,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Email = email ?? $"user{id:N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, TestWorkFactor)
        };

        return (user, password);
    }
}

public static class OrganizationFactory
{
    public static Organization CreateOrganization(string? name = null)
    {
        var id = Guid.CreateVersion7();
        
        return new Organization
        {
            Id = id,
            Name = name ?? id.ToString("N"),
            CreatedAt = DateTime.UtcNow,
        };
    }
}