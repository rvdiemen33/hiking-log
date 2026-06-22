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

/// <summary>Tier 0 tests for DELETE /hikelogs/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class DeleteHikeLogTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>DELETE /hikelogs/{id} returns 204 No Content when the hike log exists.</summary>
    [Fact]
    public async Task DeleteHikeLog_WhenExists_Returns204()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var stageResponse = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var stageId = (await stageResponse.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/hikelogs", new HikeLogFaker(stageId).Generate());
        var id = (await created.Content.ReadFromJsonAsync<HikeLogResponse>())!.Id;

        var response = await client.DeleteAsync($"/hikelogs/{id}");
        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);
    }

    /// <summary>DELETE /hikelogs/{id} returns 404 Not Found when the hike log does not exist.</summary>
    [Fact]
    public async Task DeleteHikeLog_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.DeleteAsync("/hikelogs/99999");
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
