using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpendLensDatabase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContextFactory<SpendLensDbContext>(h =>
{
    var connectionString = builder.Configuration.GetConnectionString(SpendLensDbContext.OptionName);
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    var dataSource = dataSourceBuilder.Build();
    h.UseNpgsql(dataSource);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

await app.RunAsync();