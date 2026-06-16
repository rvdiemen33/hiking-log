using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HikingLog.Infrastructure.Data;

/// <summary>Seeds the database with initial data when running in Development.</summary>
public static class DataSeeder
{
    /// <summary>Seeds routes and stages if the database is empty.</summary>
    /// <param name="serviceProvider">The application's root service provider.</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();
        var logger  = scope.ServiceProvider.GetRequiredService<ILogger<HikingLogDbContext>>();

        if (await context.Routes.AnyAsync())
        {
            logger.LogInformation("Database already contains data — skipping seed.");
            return;
        }

        logger.LogInformation("Seeding database with initial routes and stages.");

        context.Routes.AddRange(SeedData.GetRoutes());
        await context.SaveChangesAsync();

        logger.LogInformation("Seed completed successfully.");
    }
}
