namespace HikingLog.IntegrationTests.Routes.Consumers;

using HikingLog.Application.Common;
using HikingLog.Application.Routes.Commands;
using HikingLog.Infrastructure.Data;
using HikingLog.IntegrationTests.Configuration;
using HikingLog.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

/// <summary>Tier 3 tests for <see cref="AddRouteHandler"/> — verifies database persistence directly.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class AddRouteHandlerTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    private readonly HikingTestWebApplicationFactory _factory = factory;

    /// <summary>When the command is valid the handler persists the route to the database.</summary>
    [Fact]
    public async Task Handle_WhenValid_PersistsRoute()
    {
        using var scope = _factory.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>>();

        var command = new AddRoute("Noordzeepad", "LAW 1", "Nederland", 495m, "Langs de Noordzeekust");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        var db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();
        var route = await db.Routes.FindAsync(result.AsT0.Id);
        Assert.NotNull(route);
        Assert.Equal("Noordzeepad", route.Name);
        Assert.Equal("LAW 1", route.Code);
        Assert.Equal(495m, route.TotalDistanceKm);
    }
}
