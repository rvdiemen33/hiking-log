namespace HikingLog.IntegrationTests.Stages.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using HikingLog.IntegrationTests.Stages.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for POST /stages.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PostStageTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>POST /stages returns 201 Created when the request is valid and the parent route exists.</summary>
    [Fact]
    public async Task PostStage_WhenValid_Returns201()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
    }

    /// <summary>POST /stages returns 400 Bad Request when required fields are missing.</summary>
    [Fact]
    public async Task PostStage_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/stages", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    /// <summary>POST /stages returns 404 Not Found when the referenced route does not exist.</summary>
    [Fact]
    public async Task PostStage_WhenRouteNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/stages", new StageFaker(99999).Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
