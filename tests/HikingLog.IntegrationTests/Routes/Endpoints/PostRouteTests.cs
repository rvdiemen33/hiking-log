namespace HikingLog.IntegrationTests.Routes.Endpoints;

using System.Net.Http.Json;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using HikingLog.IntegrationTests.Routes.Fakers;
using Microsoft.AspNetCore.Http;

/// <summary>Tier 0 tests for POST /routes.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class PostRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    /// <summary>POST /routes returns 201 Created with Location header when the request is valid.</summary>
    [Fact]
    public async Task PostRoute_WhenValid_Returns201()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>POST /routes returns 400 Bad Request when required fields are missing.</summary>
    [Fact]
    public async Task PostRoute_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/routes", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }
}
