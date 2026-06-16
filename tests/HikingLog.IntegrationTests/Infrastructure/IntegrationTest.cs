namespace HikingLog.IntegrationTests.Infrastructure;

/// <summary>Base class for integration tests providing HTTP client helpers and per-test database reset.</summary>
public abstract class IntegrationTest(HikingTestWebApplicationFactory factory) : IAsyncLifetime
{
    /// <summary>Creates an unauthenticated HTTP client targeting the test application.</summary>
    protected HttpClient CreateClient() => factory.CreateClient();

    // Uncomment once authentication is configured:
    //
    // protected HttpClient CreateAuthenticatedClient(params string[] scopes)
    // {
    //     var client = factory.CreateClient();
    //     var token = JwtTokenFactory.Create(scopes);
    //     client.DefaultRequestHeaders.Authorization =
    //         new AuthenticationHeaderValue("Bearer", token);
    //     return client;
    // }

    /// <summary>Resets the database to a clean state before each test.</summary>
    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();

    /// <summary>No-op disposal; resource cleanup is handled by the collection fixture.</summary>
    public Task DisposeAsync() => Task.CompletedTask;
}
