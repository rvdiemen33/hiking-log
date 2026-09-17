namespace HikingLog.IntegrationTests.Stages.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.Api.Stages;
using HikingLog.Domain.Enums;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for GET /routes/{routeId}/stages.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class GetStagesByRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>GET /routes/{routeId}/stages returns 200 OK for a route with no stages.</summary>
    [Fact]
    public async Task GetStagesByRoute_WhenRouteExistsWithNoStages_Returns200()
    {
        var client = CreateClient();
        var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.GetAsync($"/routes/{routeId}/stages");
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>GET /routes/{routeId}/stages returns only that route's stages, ordered by Number.</summary>
    [Fact]
    public async Task GetStagesByRoute_ReturnsOnlyMatchingStages_OrderedByNumber()
    {
        var client = CreateClient();

        var routeA = (await (await client.PostAsJsonAsync("/routes", new RouteFaker().Generate()))
            .Content.ReadFromJsonAsync<RouteResponse>())!.Id;
        var routeB = (await (await client.PostAsJsonAsync("/routes", new RouteFaker().Generate()))
            .Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        // Route A gets three stages added out of sequence; route B gets one stage.
        foreach (var number in new[] { 3, 1, 2 })
        {
            await client.PostAsJsonAsync("/stages", StageWithNumber(routeA, number));
        }
        await client.PostAsJsonAsync("/stages", StageWithNumber(routeB, 1));

        var stages = await client.GetFromJsonAsync<List<StageResponse>>($"/routes/{routeA}/stages");

        Assert.NotNull(stages);
        Assert.Equal(3, stages!.Count);                                // only route A's stages, none from route B
        Assert.All(stages, s => Assert.Equal(routeA, s.RouteId));
        Assert.Equal(new[] { 1, 2, 3 }, stages.Select(s => s.Number)); // ordered by Number
    }

    /// <summary>GET /routes/{routeId}/stages returns each stage's own note, in Number order.</summary>
    [Fact]
    public async Task GetStagesByRoute_WhenStagesHaveMixedNotes_ReturnsNotesPerStage()
    {
        var client = CreateClient();

        var routeId = (await (await client.PostAsJsonAsync("/routes", new RouteFaker().Generate()))
            .Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        await client.PostAsJsonAsync("/stages", StageWithNumber(routeId, 1, "Let op: veerpont vaart niet in de winter"));
        await client.PostAsJsonAsync("/stages", StageWithNumber(routeId, 2, null));

        var stages = await client.GetFromJsonAsync<List<StageResponse>>($"/routes/{routeId}/stages");

        Assert.NotNull(stages);
        Assert.Equal(2, stages!.Count);
        Assert.Equal(new[] { 1, 2 }, stages.Select(s => s.Number));
        Assert.Equal("Let op: veerpont vaart niet in de winter", stages[0].Notes);
        Assert.Null(stages[1].Notes);
    }

    /// <summary>Builds a stage create request with a fixed sequence number for deterministic ordering assertions.</summary>
    /// <param name="routeId">The primary key of the parent route.</param>
    /// <param name="number">The sequence number to assign.</param>
    /// <param name="notes">The optional note to assign; defaults to <c>null</c>.</param>
    /// <returns>A valid <see cref="CreateStageRequest"/>.</returns>
    private static CreateStageRequest StageWithNumber(int routeId, int number, string? notes = null)
        => new(routeId, number, $"Etappe {number}", "Start", "Einde", 10m, 100m, Difficulty.Easy, notes);
}
