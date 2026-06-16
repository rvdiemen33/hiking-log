using HikingLog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Testcontainers.MsSql;

namespace HikingLog.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory for integration tests. Starts a SQL Server container via Testcontainers,
/// replaces the production DbContext registration, applies migrations, and initialises Respawn
/// for fast per-test database resets without restarting the container.
/// </summary>
public class HikingTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private Respawner _respawner = null!;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<HikingLogDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<HikingLogDbContext>(opts =>
                opts.UseSqlServer(_sqlContainer.GetConnectionString()));
        });
    }

    /// <summary>Returns the connection string for the SQL Server test container.</summary>
    public string GetConnectionString() => _sqlContainer.GetConnectionString();

    /// <summary>Truncates all user tables using Respawn, resetting the database between tests.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(_sqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>Starts the SQL Server container, applies migrations, and initialises Respawn.</summary>
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        // Apply migrations directly before accessing Services, so that when the app starts
        // (via Services.CreateScope below) and DataSeeder runs, the tables already exist.
        var opts = new DbContextOptionsBuilder<HikingLogDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;
        await using (var db = new HikingLogDbContext(opts))
            await db.Database.MigrateAsync();

        // Accessing Services starts the ASP.NET Core host, which runs Program.cs including DataSeeder.
        using var scope = Services.CreateScope();

        await using var connection = new SqlConnection(_sqlContainer.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer
        });
    }

    /// <summary>Stops and disposes the SQL Server container.</summary>
    public new async Task DisposeAsync() => await _sqlContainer.DisposeAsync();
}
