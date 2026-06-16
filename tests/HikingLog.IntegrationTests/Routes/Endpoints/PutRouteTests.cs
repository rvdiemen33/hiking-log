namespace HikingLog.IntegrationTests.Routes.Endpoints;

using System.Net.Http.Json;
using HikingLog.Api.Routes;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for PUT /routes/{id}.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PutRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>PUT /routes/{id} returns 200 OK when the request is valid and the route exists.</summary>
    [Fact]
    public async Task PutRoute_WhenValid_Returns200()
    {
        var client = CreateClient();
        var created = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var id = (await created.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.PutAsJsonAsync($"/routes/{id}", new RouteFaker().Generate());
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    /// <summary>PUT /routes/{id} returns 400 Bad Request when required fields are missing.</summary>
    [Fact]
    public async Task PutRoute_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/routes/1", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    /// <summary>PUT /routes/{id} returns 404 Not Found when the route does not exist.</summary>
    [Fact]
    public async Task PutRoute_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/routes/99999", new RouteFaker().Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
