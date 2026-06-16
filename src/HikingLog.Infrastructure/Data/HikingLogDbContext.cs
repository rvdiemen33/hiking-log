using HikingLog.Application.Data.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HikingLog.Infrastructure.Data;

/// <summary>Entity Framework Core database context for the HikingLog application.</summary>
public class HikingLogDbContext(DbContextOptions<HikingLogDbContext> options)
    : DbContext(options), IHikingLogDataContext
{
}
