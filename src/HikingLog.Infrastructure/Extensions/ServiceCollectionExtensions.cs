namespace HikingLog.Infrastructure.Extensions;

using HikingLog.Application.Data.Contracts;
using HikingLog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Infrastructure layer services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the database context and related Infrastructure services.</summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration containing the connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<HikingLogDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("HikingLog")));

        services.AddScoped<IHikingLogDataContext, HikingLogDbContext>();

        return services;
    }
}
