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

    /// <summary>Builds a stage create request with a fixed sequence number for deterministic ordering assertions.</summary>
    /// <param name="routeId">The primary key of the parent route.</param>
    /// <param name="number">The sequence number to assign.</param>
    /// <returns>A valid <see cref="CreateStageRequest"/>.</returns>
    private static CreateStageRequest StageWithNumber(int routeId, int number)
        => new(routeId, number, $"Etappe {number}", "Start", "Einde", 10m, 100m, Difficulty.Easy);
}
