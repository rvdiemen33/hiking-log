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

/// <summary>Tier 0 tests for PUT /stages/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PutStageTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>PUT /stages/{id} returns 200 OK when the request is valid and the stage exists.</summary>
    [Fact]
    public async Task PutStage_WhenValid_Returns200()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var id = (await created.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var response = await client.PutAsJsonAsync($"/stages/{id}", new StageFaker(routeId).Generate());
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>PUT /stages/{id} returns 400 Bad Request when required fields are missing.</summary>
    [Fact]
    public async Task PutStage_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/stages/1", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    /// <summary>PUT /stages/{id} returns 404 Not Found when the stage does not exist.</summary>
    [Fact]
    public async Task PutStage_WhenNotFound_Returns404()
    {
        var client = CreateClient();

        // We still need a valid route to pass validation, but the stage id is non-existent.
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.PutAsJsonAsync("/stages/99999", new StageFaker(routeId).Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    /// <summary>PUT /stages/{id} returns 404 Not Found when the referenced parent route does not exist.</summary>
    [Fact]
    public async Task PutStage_WhenRouteNotFound_Returns404()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var id = (await created.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        // Reparent the existing stage to a route that does not exist.
        var response = await client.PutAsJsonAsync($"/stages/{id}", new StageFaker(99999).Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    /// <summary>PUT /stages/{id} returns 200 OK and echoes the note verbatim when one is provided (AC3.1).</summary>
    [Fact]
    public async Task PutStage_WhenNotesProvided_Returns200AndEchoesNotes()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var id = (await created.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var request = new StageFaker(routeId).Generate() with { Notes = "Herberg onderweg gesloten op maandag" };
        var response = await client.PutAsJsonAsync($"/stages/{id}", request);

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Equal("Herberg onderweg gesloten op maandag", body!.Notes);

        // The PUT response echoes the request, so read the stage back to prove the note was persisted.
        var getResponse = await client.GetAsync($"/stages/{id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Equal("Herberg onderweg gesloten op maandag", getBody!.Notes);
    }

    /// <summary>PUT /stages/{id} clears an existing note when the request body literally omits the notes property (AC3.2, AC6.1).</summary>
    [Fact]
    public async Task PutStage_WhenNotesOmitted_ClearsNotes()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var createRequest = new StageFaker(routeId).Generate() with { Notes = "x" };
        var created = await client.PostAsJsonAsync("/stages", createRequest);
        var id = (await created.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        // Anonymous object without a "notes" property — literally omitted, not sent as null.
        var bodyWithoutNotes = new
        {
            createRequest.RouteId,
            createRequest.Number,
            createRequest.Name,
            createRequest.StartPoint,
            createRequest.EndPoint,
            createRequest.DistanceKm,
            createRequest.ElevationDifferenceM,
            createRequest.Difficulty
        };
        var response = await client.PutAsJsonAsync($"/stages/{id}", bodyWithoutNotes);

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var putBody = await response.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Null(putBody!.Notes);

        var getResponse = await client.GetAsync($"/stages/{id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Null(getBody!.Notes);
    }

    /// <summary>PUT /stages/{id} keeps an empty string note as an empty string, not coerced to null (AC6.2).</summary>
    [Fact]
    public async Task PutStage_WhenNotesIsEmptyString_KeepsEmptyString()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var createRequest = new StageFaker(routeId).Generate() with { Notes = "x" };
        var created = await client.PostAsJsonAsync("/stages", createRequest);
        var id = (await created.Content.ReadFromJsonAsync<StageResponse>())!.Id;

        var request = createRequest with { Notes = string.Empty };
        var response = await client.PutAsJsonAsync($"/stages/{id}", request);

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var putBody = await response.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Equal(string.Empty, putBody!.Notes);

        var getResponse = await client.GetAsync($"/stages/{id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Equal(string.Empty, getBody!.Notes);
    }

    /// <summary>PUT /stages/{id} returns 400 Bad Request when the note exceeds the maximum length (AC3.3).</summary>
    [Fact]
    public async Task PutStage_WhenNotesTooLong_Returns400()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var created = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
        var original = (await created.Content.ReadFromJsonAsync<StageResponse>())!;

        var request = new StageFaker(routeId).Generate() with { Notes = new string('n', 2001) };
        var response = await client.PutAsJsonAsync($"/stages/{original.Id}", request);

        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(UpdateStageRequest.Notes), problem!.Errors.Keys);

        // The rejected request must not have modified the stage.
        var getResponse = await client.GetAsync($"/stages/{original.Id}");
        var unchanged = await getResponse.Content.ReadFromJsonAsync<StageResponse>();
        Assert.Equal(original.Name, unchanged!.Name);
        Assert.Equal(original.Notes, unchanged.Notes);
    }
}
