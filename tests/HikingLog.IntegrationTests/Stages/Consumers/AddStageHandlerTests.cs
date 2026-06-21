namespace HikingLog.IntegrationTests.Stages.Consumers;

using Application.Common;
using Application.Routes.Commands;
using Application.Stages.Commands;
using Configuration;
using HikingLog.Infrastructure.Data;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

/// <summary>Tier 3 tests for <see cref="AddStageHandler"/> — verifies database persistence directly.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class AddStageHandlerTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    private readonly HikingTestWebApplicationFactory _factory = factory;

    /// <summary>When the command is valid the handler persists the stage to the database.</summary>
    [Fact]
    public async Task Handle_WhenValid_PersistsStage()
    {
        using IServiceScope scope = _factory.Services.CreateScope();

        // Seed a parent route first so the stage FK constraint is satisfied.
        ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> routeHandler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>>();
        OneOf<AddRouteResult, ValidationFailed> routeResult = await routeHandler.Handle(
            new AddRoute("Testroute", "TR 1", "Nederland", 100m, null), CancellationToken.None);
        Assert.True(routeResult.IsT0);
        int routeId = routeResult.AsT0.Id;

        // Now add the stage under that route.
        ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>> stageHandler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>>>();

        var command = new AddStage(routeId, 1, "Etappe 1", "Bergen", "Haarlem", 22.5m, 150m, Domain.Enums.Difficulty.Moderate);
        OneOf<AddStageResult, ValidationFailed, NotFound> result = await stageHandler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        HikingLogDbContext db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();
        Domain.Entities.Stage? stage = await db.Stages.FindAsync(result.AsT0.Id);
        Assert.NotNull(stage);
        Assert.Equal("Etappe 1", stage.Name);
        Assert.Equal(routeId, stage.RouteId);
    }
}
