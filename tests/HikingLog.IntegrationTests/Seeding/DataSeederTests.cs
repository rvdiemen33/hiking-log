namespace HikingLog.IntegrationTests.Seeding;

using Configuration;
using Domain.Entities;
using HikingLog.Infrastructure.Data;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Tier 3 tests that verify <see cref="DataSeeder" /> persists the correct seed data.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class DataSeederTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    // Respawn clears the database in IntegrationTest.InitializeAsync before each test,
    // so every test starts with an empty database and can invoke the seeder fresh.
    private readonly HikingTestWebApplicationFactory _factory = factory;

    /// <summary>Verifies that the seeder inserts 2 routes and 50 stages into an empty database.</summary>
    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_InsertsExpectedRoutesAndStageCount()
    {
        await DataSeeder.SeedAsync(_factory.Services);

        using IServiceScope scope = _factory.Services.CreateScope();
        HikingLogDbContext db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();

        Assert.Equal(2, await db.Routes.CountAsync());
        Assert.Equal(50, await db.Stages.CountAsync());
    }

    /// <summary>Verifies that calling the seeder a second time does not insert duplicate data.</summary>
    [Fact]
    public async Task SeedAsync_WhenCalledTwice_DoesNotInsertDuplicates()
    {
        await DataSeeder.SeedAsync(_factory.Services);
        await DataSeeder.SeedAsync(_factory.Services);

        using IServiceScope scope = _factory.Services.CreateScope();
        HikingLogDbContext db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();

        Assert.Equal(2, await db.Routes.CountAsync());
        Assert.Equal(50, await db.Stages.CountAsync());
    }

    /// <summary>Verifies that the Pieterpad route is seeded with the correct attributes and 26 stages.</summary>
    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_SeedsPieterpadWith26Stages()
    {
        await DataSeeder.SeedAsync(_factory.Services);

        using IServiceScope scope = _factory.Services.CreateScope();
        HikingLogDbContext db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();

        Route route = await db
                            .Routes
                            .Include(r => r.Stages)
                            .SingleAsync(r => r.Code == "LAW 9");

        Assert.Equal("Pieterpad", route.Name);
        Assert.Equal("Nederland", route.Country);
        Assert.Equal(497m, route.TotalDistanceKm);
        Assert.Equal(26, route.Stages.Count);
    }

    /// <summary>Verifies that the Trekvogelpad route is seeded with the correct attributes and 24 stages.</summary>
    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_SeedsTrekvogelpadWith24Stages()
    {
        await DataSeeder.SeedAsync(_factory.Services);

        using IServiceScope scope = _factory.Services.CreateScope();
        HikingLogDbContext db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();

        Route route = await db
                            .Routes
                            .Include(r => r.Stages)
                            .SingleAsync(r => r.Code == "LAW 2");

        Assert.Equal("Trekvogelpad", route.Name);
        Assert.Equal("Nederland", route.Country);
        Assert.Equal(414m, route.TotalDistanceKm);
        Assert.Equal(24, route.Stages.Count);
    }
}
