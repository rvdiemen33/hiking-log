namespace HikingLog.IntegrationTests.Stages.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using HikingLog.IntegrationTests.Stages.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for GET /stages/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class GetStageTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>GET /stages/{id} returns 200 OK when the stage exists.</summary>
    [Fact]
    public async Task GetStage_WhenExists_Returns200()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var location = created.Headers.Location!;

        var response = await client.GetAsync(location);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>GET /stages/{id} returns 404 Not Found when the stage does not exist.</summary>
    [Fact]
    public async Task GetStage_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/stages/99999");
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
