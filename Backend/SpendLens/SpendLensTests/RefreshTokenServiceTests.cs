using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensDatabase;
using SpendLensDatabase.Models.Entities;

namespace SpendLensTests;

[Collection("Integration")]
public sealed class RefreshTokenServiceTests: IAsyncDisposable
{
    private readonly PostgresFixture _postgresFixture;
    private readonly SpendLensDbContext _dbContext;
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly RefreshTokenService _refreshTokenService;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public RefreshTokenServiceTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        _dbContext = postgresFixture.CreateContext();
        
        _refreshTokenService = new RefreshTokenService(_dbContext,
            NullLogger<RefreshTokenService>.Instance);
    }

    [Fact]
    public async Task AddRefreshTokenAndVerify_WithValidHash_ReturnTrue()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);
        var rawToken = _refreshTokenService.AddRefreshToken(RefreshTokenLifetime, user);
        
        await _dbContext.SaveChangesAsync(_cancellationToken);

        var (id, verifier) = RefreshTokenGenerator.Split(rawToken);
        
        var refreshToken = _dbContext.RefreshTokens.First(x => x.Id == id);

        var hash = SHA256.HashData(verifier.ToByteArray());
        
        var result = CryptographicOperations.FixedTimeEquals(hash, refreshToken.TokenHash);
        
        Assert.True(result);
    }

    [Fact]
    public async Task AddRefreshTokenAndVerify_WithInvalidHash_ReturnFalse()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);
        var rawToken = _refreshTokenService.AddRefreshToken(RefreshTokenLifetime, user);
        
        await _dbContext.SaveChangesAsync(_cancellationToken);

        var (id, verifier) = RefreshTokenGenerator.Split(rawToken);
        
        var refreshToken = _dbContext.RefreshTokens.First(x => x.Id == id);

        var hash = SHA256.HashData(verifier.ToByteArray());
        hash[0] = unchecked((byte)(hash[0] + 1));
        
        var result = CryptographicOperations.FixedTimeEquals(hash, refreshToken.TokenHash);
        
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_WithValidData_ReturnSuccess()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);
        var rawToken = _refreshTokenService.AddRefreshToken(RefreshTokenLifetime, user);

        await _dbContext.SaveChangesAsync(_cancellationToken);
        
        var (id, verifier) = RefreshTokenGenerator.Split(rawToken);

        var validationResult = await _refreshTokenService.ValidateAsync(id, verifier, _cancellationToken);
        
        Assert.IsType<RefreshTokenValidationResult.Success>(validationResult);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidId_ReturnNotFound()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);
        var rawToken = _refreshTokenService.AddRefreshToken(RefreshTokenLifetime, user);

        await _dbContext.SaveChangesAsync(_cancellationToken);
        
        var (_, verifier) = RefreshTokenGenerator.Split(rawToken);
        
        var validationResult  = await _refreshTokenService.ValidateAsync(Guid.CreateVersion7(), verifier, _cancellationToken);
        
        Assert.IsType<RefreshTokenValidationResult.NotFound>(validationResult);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidIdentifier_ReturnInvalid()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);

        var rawToken = _refreshTokenService.AddRefreshToken(RefreshTokenLifetime, user);

        await _dbContext.SaveChangesAsync(_cancellationToken);
        
        var (id, _) = RefreshTokenGenerator.Split(rawToken);
        
        var validationResult  = await _refreshTokenService.ValidateAsync(id, Guid.CreateVersion7(), _cancellationToken);
        
        Assert.IsType<RefreshTokenValidationResult.Invalid>(validationResult);
    }

    [Fact]
    public async Task ValidateAsync_Revoked5MinuteAgo_ReturnReuseDetected()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);

        var (rawToken, refreshToken) = RefreshTokenFactory.CreateRefreshToken(user.Id, revokedAt: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(_cancellationToken);

        var (id, verifier) = RefreshTokenGenerator.Split(rawToken);

        var validationResult = await _refreshTokenService.ValidateAsync(id, verifier, _cancellationToken);

        Assert.IsType<RefreshTokenValidationResult.ReuseDetected>(validationResult);
    }

    [Fact]
    public async Task ValidateAsync_Revoked10SecondsAgo_ReturnInvalid()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);

        var (rawToken, refreshToken) = RefreshTokenFactory.CreateRefreshToken(user.Id, revokedAt: DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10)));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(_cancellationToken);

        var (id, verifier) = RefreshTokenGenerator.Split(rawToken);

        var validationResult = await _refreshTokenService.ValidateAsync(id, verifier, _cancellationToken);

        Assert.IsType<RefreshTokenValidationResult.Invalid>(validationResult);
    }

    [Fact]
    public async Task ValidateAsync_ExpiredToken_ReturnInvalid()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);

        var (rawToken, refreshToken) = RefreshTokenFactory.CreateRefreshToken(user.Id,
            expiresAt: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(_cancellationToken);

        var (id, verifier) = RefreshTokenGenerator.Split(rawToken);

        var validationResult = await _refreshTokenService.ValidateAsync(id, verifier, _cancellationToken);

        Assert.IsType<RefreshTokenValidationResult.Invalid>(validationResult);
    }

    [Fact]
    public async Task RotateAsync_ValidData_ReturnSuccess()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);

        var (rawToken, refreshToken) = RefreshTokenFactory.CreateRefreshToken(user.Id);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(_cancellationToken);

        var (id, _) = RefreshTokenGenerator.Split(rawToken);

        var result = await _refreshTokenService.RotateAsync(id, RefreshTokenLifetime, _cancellationToken);
        
        Assert.IsType<RefreshResult.Success>(result);
    }

    [Fact]
    public async Task RotateAsync_AbsentToken_ReturnReuseOrNotFound()
    {
        var result = await _refreshTokenService.RotateAsync(Guid.CreateVersion7(), RefreshTokenLifetime, _cancellationToken);
        
        Assert.IsType<RefreshResult.ReuseOrNotFound>(result);
    }

    [Fact]
    public async Task RotateAsync_RevokedToken_ReturnReuseOrNotFound()
    {
        var (user, _) = await TestDataSeeder.SeedUserAsync(_dbContext, cancellationToken: _cancellationToken);

        var (rawToken, refreshToken) = RefreshTokenFactory.CreateRefreshToken(user.Id,
            revokedAt: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(_cancellationToken);

        var (id, _) = RefreshTokenGenerator.Split(rawToken);

        var result = await _refreshTokenService.RotateAsync(id, RefreshTokenLifetime, _cancellationToken);
        
        Assert.IsType<RefreshResult.ReuseOrNotFound>(result);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresFixture.ResetAsync();
    }
}

public static class RefreshTokenFactory
{
    private static readonly TimeSpan LifeTime = Timeout.InfiniteTimeSpan;
    
    public static (string RawToken, RefreshToken RefreshToken) CreateRefreshToken(Guid userId,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null)
    {
        var (rawToken, id, verifierHash) = RefreshTokenGenerator.Generate();
        return (rawToken, new RefreshToken
        {
            Id = id,
            ExpiresAt = expiresAt ??  DateTime.UtcNow.Add(LifeTime),
            TokenHash = verifierHash,
            UserId = userId,
            RevokedAt = revokedAt
        });
    }
}