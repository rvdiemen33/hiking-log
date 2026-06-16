namespace HikingLog.IntegrationTests.Routes.Endpoints;

using System.Net.Http.Json;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for GET /routes.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class GetRoutesTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>GET /routes returns 200 OK when no routes exist.</summary>
    [Fact]
    public async Task GetRoutes_WhenNoRoutesExist_Returns200()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/routes");
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>GET /routes returns 200 OK when routes exist.</summary>
    [Fact]
    public async Task GetRoutes_WhenRoutesExist_Returns200()
    {
        var client = CreateClient();
        await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());

        var response = await client.GetAsync("/routes");
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }
}
