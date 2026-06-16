namespace HikingLog.IntegrationTests.Routes.Consumers;

using Application.Common;
using Application.Routes.Commands;
using Configuration;
using Domain.Entities;
using HikingLog.Infrastructure.Data;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

/// <summary>Tier 3 tests for <see cref="AddRouteHandler" /> — verifies database persistence directly.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class AddRouteHandlerTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    private readonly HikingTestWebApplicationFactory _factory = factory;

    /// <summary>When the command is valid the handler persists the route to the database.</summary>
    [Fact]
    public async Task Handle_WhenValid_PersistsRoute()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>>();

        var command = new AddRoute("Noordzeepad", "LAW 1", "Nederland", 495m, "Langs de Noordzeekust");
        OneOf<AddRouteResult, ValidationFailed> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        HikingLogDbContext db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();
        Route? route = await db.Routes.FindAsync(result.AsT0.Id);
        Assert.NotNull(route);
        Assert.Equal("Noordzeepad", route.Name);
        Assert.Equal("LAW 1", route.Code);
        Assert.Equal(495m, route.TotalDistanceKm);
    }
}
