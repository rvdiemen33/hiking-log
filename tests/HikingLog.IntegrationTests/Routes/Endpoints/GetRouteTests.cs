namespace HikingLog.IntegrationTests.Routes.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for GET /routes/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class GetRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>GET /routes/{id} returns 200 OK when the route exists.</summary>
    [Fact]
    public async Task GetRoute_WhenExists_Returns200()
    {
        var client = CreateClient();
        var created = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var location = created.Headers.Location!;

        var response = await client.GetAsync(location);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>GET /routes/{id} returns 404 Not Found when the route does not exist.</summary>
    [Fact]
    public async Task GetRoute_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/routes/99999");
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
