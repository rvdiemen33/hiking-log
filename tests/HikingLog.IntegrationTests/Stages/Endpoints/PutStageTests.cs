namespace HikingLog.IntegrationTests.Stages.Endpoints;

using System.Net.Http.Json;
using Api.Routes;
using Api.Stages;
using Configuration;
using Fakers;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Routes.Fakers;

/// <summary>Tier 0 tests for PUT /stages/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PutStageTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>PUT /stages/{id} returns 200 OK when the request is valid and the stage exists.</summary>
    [Fact]
    public async Task PutStage_WhenValid_Returns200()
    {
        HttpClient client = CreateClient();
        HttpResponseMessage routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        int routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        HttpResponseMessage created = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        int id = (await created.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        HttpResponseMessage response = await client.PutAsJsonAsync($"/stages/{id}", new StageFaker(routeId).Generate());
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>PUT /stages/{id} returns 400 Bad Request when required fields are missing.</summary>
    [Fact]
    public async Task PutStage_WhenInvalid_Returns400()
    {
        HttpClient client = CreateClient();
        HttpResponseMessage response = await client.PutAsJsonAsync("/stages/1", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    /// <summary>PUT /stages/{id} returns 404 Not Found when the stage does not exist.</summary>
    [Fact]
    public async Task PutStage_WhenNotFound_Returns404()
    {
        HttpClient client = CreateClient();

        // We still need a valid route to pass validation, but the stage id is non-existent.
        HttpResponseMessage routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        int routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        HttpResponseMessage response = await client.PutAsJsonAsync("/stages/99999", new StageFaker(routeId).Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
