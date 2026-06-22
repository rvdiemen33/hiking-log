namespace HikingLog.IntegrationTests.HikeLogs.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.HikeLogs;
using HikingLog.Api.Routes;
using HikingLog.Api.Stages;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.HikeLogs.Fakers;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using HikingLog.IntegrationTests.Stages.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for PUT /hikelogs/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PutHikeLogTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>PUT /hikelogs/{id} returns 200 OK when the request is valid and the hike log exists.</summary>
    [Fact]
    public async Task PutHikeLog_WhenValid_Returns200()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var stageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var stageId = (await stageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(stageId).Generate());
        var id = (await created.Content.ReadFromJsonAsync<HikeLogResponse>())!.Id;

        var response = await client.PutAsJsonAsync($"/hikelogs/{id}", new HikeLogFaker(stageId).Generate());
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>PUT /hikelogs/{id} returns 400 Bad Request when required fields are missing.</summary>
    [Fact]
    public async Task PutHikeLog_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/hikelogs/1", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    /// <summary>PUT /hikelogs/{id} returns 404 Not Found when the hike log does not exist.</summary>
    [Fact]
    public async Task PutHikeLog_WhenNotFound_Returns404()
    {
        var client = CreateClient();

        // A valid stage is needed to pass validation, but the hike log id is non-existent.
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var stageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var stageId = (await stageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var response = await client.PutAsJsonAsync("/hikelogs/99999", new HikeLogFaker(stageId).Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    /// <summary>PUT /hikelogs/{id} returns 404 Not Found when the referenced parent stage does not exist.</summary>
    [Fact]
    public async Task PutHikeLog_WhenStageNotFound_Returns404()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var stageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var stageId = (await stageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(stageId).Generate());
        var id = (await created.Content.ReadFromJsonAsync<HikeLogResponse>())!.Id;

        // Reparent the existing hike log to a stage that does not exist.
        var response = await client.PutAsJsonAsync($"/hikelogs/{id}", new HikeLogFaker(99999).Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
