namespace HikingLog.IntegrationTests.Stages.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for GET /routes/{routeId}/stages.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class GetStagesByRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>GET /routes/{routeId}/stages returns 200 OK for a route with no stages.</summary>
    [Fact]
    public async Task GetStagesByRoute_WhenRouteExistsWithNoStages_Returns200()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.GetAsync($"/routes/{routeId}/stages");
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }
}
