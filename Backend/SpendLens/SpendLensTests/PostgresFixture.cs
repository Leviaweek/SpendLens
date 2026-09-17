using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using SpendLensDatabase;
using Testcontainers.PostgreSql;

namespace SpendLensTests;

public sealed class PostgresFixture: IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    private Respawner _respawner = null!;
    
    public string ConnectionString => _container.GetConnectionString();

    public SpendLensDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SpendLensDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SpendLensDbContext(options);
    }
    
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync(_cancellationToken);
        
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(_cancellationToken);
        
        await using var connection =
            new NpgsqlConnection(ConnectionString);

        await connection.OpenAsync(_cancellationToken);

        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres
            });
    }
    
    public async Task ResetAsync()
    {
        await using var connection =
            new NpgsqlConnection(ConnectionString);

        await connection.OpenAsync(_cancellationToken);

        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}