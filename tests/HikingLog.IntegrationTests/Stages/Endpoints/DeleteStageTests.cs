namespace HikingLog.IntegrationTests.Stages.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.Api.Stages;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using HikingLog.IntegrationTests.Stages.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for DELETE /stages/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class DeleteStageTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>DELETE /stages/{id} returns 204 No Content when the stage exists.</summary>
    [Fact]
    public async Task DeleteStage_WhenExists_Returns204()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var id = (await created.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var response = await client.DeleteAsync($"/stages/{id}");
        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);
    }

    /// <summary>DELETE /stages/{id} returns 404 Not Found when the stage does not exist.</summary>
    [Fact]
    public async Task DeleteStage_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.DeleteAsync("/stages/99999");
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
