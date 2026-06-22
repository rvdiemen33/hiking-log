namespace HikingLog.IntegrationTests.HikeLogs.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.Api.Stages;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.HikeLogs.Fakers;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using HikingLog.IntegrationTests.Stages.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for POST /hikelogs.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PostHikeLogTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>POST /hikelogs returns 201 Created when the request is valid and the parent stage exists.</summary>
    [Fact]
    public async Task PostHikeLog_WhenValid_Returns201()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var stageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var stageId = (await stageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var response = await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(stageId).Generate());
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
    }

    /// <summary>POST /hikelogs returns 400 Bad Request when required fields are missing.</summary>
    [Fact]
    public async Task PostHikeLog_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/hikelogs", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    /// <summary>POST /hikelogs returns 404 Not Found when the referenced stage does not exist.</summary>
    [Fact]
    public async Task PostHikeLog_WhenStageNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(99999).Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
