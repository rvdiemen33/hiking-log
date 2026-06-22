namespace HikingLog.IntegrationTests.HikeLogs.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.HikeLogs;
using HikingLog.Api.Routes;
using HikingLog.Api.Stages;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using HikingLog.IntegrationTests.Stages.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for GET /hikelogs.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class GetHikeLogsTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>GET /hikelogs returns 200 OK even when there are no logs.</summary>
    [Fact]
    public async Task GetHikeLogs_WhenNoLogs_Returns200()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/hikelogs");
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>GET /hikelogs returns the persisted logs when logs exist.</summary>
    [Fact]
    public async Task GetHikeLogs_WhenLogsExist_ReturnsThem()
    {
        var client = CreateClient();
        var stageId = await CreateStageAsync(client);

        await client.PostAsJsonAsync("/hikelogs", new CreateHikeLogRequest(stageId, new DateOnly(2024, 6, 1), 120, "Sunny", null, 4));
        await client.PostAsJsonAsync("/hikelogs", new CreateHikeLogRequest(stageId, new DateOnly(2024, 7, 1), 90, "Cloudy", null, 3));

        var logs = await client.GetFromJsonAsync<List<HikeLogResponse>>("/hikelogs");

        Assert.NotNull(logs);
        Assert.Equal(2, logs!.Count);
    }

    /// <summary>GET /hikelogs?year=N returns only logs hiked in that year.</summary>
    [Fact]
    public async Task GetHikeLogs_WhenFilteredByYear_ReturnsOnlyThatYear()
    {
        var client = CreateClient();
        var stageId = await CreateStageAsync(client);

        await client.PostAsJsonAsync("/hikelogs", new CreateHikeLogRequest(stageId, new DateOnly(2023, 5, 10), 120, "Sunny", null, 4));
        await client.PostAsJsonAsync("/hikelogs", new CreateHikeLogRequest(stageId, new DateOnly(2024, 5, 10), 120, "Sunny", null, 4));
        await client.PostAsJsonAsync("/hikelogs", new CreateHikeLogRequest(stageId, new DateOnly(2024, 8, 20), 90, "Cloudy", null, 3));

        var logs = await client.GetFromJsonAsync<List<HikeLogResponse>>("/hikelogs?year=2024");

        Assert.NotNull(logs);
        Assert.Equal(2, logs!.Count);
        Assert.All(logs, log => Assert.Equal(2024, log.DateHiked.Year));
    }

    /// <summary>Creates a route and a stage, returning the stage id to use as a parent for hike logs.</summary>
    /// <param name="client">The HTTP client used to issue the setup requests.</param>
    /// <returns>The primary key of the newly created stage.</returns>
    private static async Task<int> CreateStageAsync(HttpClient client)
    {
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var stageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        return (await stageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;
    }
}
