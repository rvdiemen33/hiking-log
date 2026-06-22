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

/// <summary>Tier 0 tests for GET /stages/{stageId}/hikelogs.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class GetHikeLogsByStageTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>GET /stages/{stageId}/hikelogs returns 200 OK with an empty list when the stage exists but has no logs.</summary>
    [Fact]
    public async Task GetHikeLogsByStage_WhenStageExistsWithNoLogs_Returns200()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var stageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var stageId = (await stageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var response = await client.GetAsync($"/stages/{stageId}/hikelogs");
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>GET /stages/{stageId}/hikelogs returns only the logs belonging to the requested stage, excluding logs of other stages.</summary>
    [Fact]
    public async Task GetHikeLogsByStage_WhenLogsExistForMultipleStages_ReturnsOnlyTheRequestedStage()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var firstStageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var firstStageId = (await firstStageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var secondStageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var secondStageId = (await secondStageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        // Two logs for the requested stage, one for a different stage that must be excluded.
        await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(firstStageId).Generate());
        await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(firstStageId).Generate());
        await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(secondStageId).Generate());

        var logs = await client.GetFromJsonAsync<List<HikeLogResponse>>($"/stages/{firstStageId}/hikelogs");

        Assert.NotNull(logs);
        Assert.Equal(2, logs!.Count);
        Assert.All(logs, log => Assert.Equal(firstStageId, log.StageId));
    }
}
