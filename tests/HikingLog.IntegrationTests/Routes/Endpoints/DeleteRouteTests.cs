namespace HikingLog.IntegrationTests.Routes.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for DELETE /routes/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class DeleteRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>DELETE /routes/{id} returns 204 No Content when the route exists.</summary>
    [Fact]
    public async Task DeleteRoute_WhenExists_Returns204()
    {
        var client = CreateClient();
        var created = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var id = (await created.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.DeleteAsync($"/routes/{id}");
        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);
    }

    /// <summary>DELETE /routes/{id} returns 404 Not Found when the route does not exist.</summary>
    [Fact]
    public async Task DeleteRoute_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.DeleteAsync("/routes/99999");
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
