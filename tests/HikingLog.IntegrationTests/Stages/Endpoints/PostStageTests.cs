namespace HikingLog.IntegrationTests.Stages.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.Api.Stages;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using HikingLog.IntegrationTests.Stages.Fakers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Tier 0 tests for POST /stages.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PostStageTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>POST /stages returns 201 Created when the request is valid and the parent route exists, with a null note (AC2.2).</summary>
    [Fact]
    public async Task PostStage_WhenValid_Returns201()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Null(body!.Notes);
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

    /// <summary>POST /stages returns 201 Created and echoes the note verbatim when one is provided (AC2.1).</summary>
    [Fact]
    public async Task PostStage_WhenNotesProvided_Returns201AndEchoesNotes()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var request = new StageFaker(routeId).Generate() with { Notes = "Mooie etappe langs de Berkel" };
        var response = await client.PostAsJsonAsync("/stages", request);

        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Equal("Mooie etappe langs de Berkel", body!.Notes);
    }

    /// <summary>POST /stages returns 400 Bad Request when the note exceeds the maximum length (AC2.3).</summary>
    [Fact]
    public async Task PostStage_WhenNotesTooLong_Returns400()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var request = new StageFaker(routeId).Generate() with { Notes = new string('n', 2001) };
        var response = await client.PostAsJsonAsync("/stages", request);

        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateStageRequest.Notes), problem!.Errors.Keys);

        // The rejected request must not have created a stage on the route.
        var stages = await client.GetFromJsonAsync<List<StageResponse>>($"/routes/{routeId}/stages");
        Assert.Empty(stages!);
    }
}
