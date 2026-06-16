namespace HikingLog.IntegrationTests.Configuration;

using HikingLog.IntegrationTests.Infrastructure;

/// <summary>xUnit collection for Tier 0 and Tier 3 integration tests sharing a single SQL Server container instance.</summary>
[CollectionDefinition(nameof(HikingLogTier0Collection))]
public class HikingLogTier0Collection : ICollectionFixture<HikingTestWebApplicationFactory> { }
