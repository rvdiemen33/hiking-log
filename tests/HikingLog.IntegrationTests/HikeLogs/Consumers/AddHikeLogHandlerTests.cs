namespace HikingLog.IntegrationTests.HikeLogs.Consumers;

using Application.Common;
using Application.HikeLogs.Commands;
using Application.Routes.Commands;
using Application.Stages.Commands;
using Configuration;
using HikingLog.Infrastructure.Data;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

/// <summary>Tier 3 tests for <see cref="AddHikeLogHandler"/> — verifies database persistence directly.</summary>
[Collection(nameof(HikingLogTier0Collection))]
public class AddHikeLogHandlerTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    private readonly HikingTestWebApplicationFactory _factory = factory;

    /// <summary>When the command is valid the handler persists the hike log to the database.</summary>
    [Fact]
    public async Task Handle_WhenValid_PersistsHikeLog()
    {
        using IServiceScope scope = _factory.Services.CreateScope();

        // Seed a parent route first.
        ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> routeHandler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>>();
        OneOf<AddRouteResult, ValidationFailed> routeResult = await routeHandler.Handle(
            new AddRoute("Testroute", "TR 1", "Nederland", 100m, null), CancellationToken.None);
        Assert.True(routeResult.IsT0);
        int routeId = routeResult.AsT0.Id;

        // Seed a parent stage under that route.
        ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>> stageHandler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>>>();
        OneOf<AddStageResult, ValidationFailed, NotFound> stageResult = await stageHandler.Handle(
            new AddStage(routeId, 1, "Etappe 1", "Bergen", "Haarlem", 22.5m, 150m, Domain.Enums.Difficulty.Moderate),
            CancellationToken.None);
        Assert.True(stageResult.IsT0);
        int stageId = stageResult.AsT0.Id;

        // Now add the hike log under that stage.
        ICommandHandler<AddHikeLog, OneOf<AddHikeLogResult, ValidationFailed, NotFound>> hikeLogHandler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddHikeLog, OneOf<AddHikeLogResult, ValidationFailed, NotFound>>>();

        var command = new AddHikeLog(stageId, DateOnly.FromDateTime(DateTime.Today), 120, "Sunny", null, 4);
        OneOf<AddHikeLogResult, ValidationFailed, NotFound> result = await hikeLogHandler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        HikingLogDbContext db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();
        Domain.Entities.HikeLog? hikeLog = await db.HikeLogs.FindAsync(result.AsT0.Id);
        Assert.NotNull(hikeLog);
        Assert.Equal(stageId, hikeLog.StageId);
        Assert.Equal(4, hikeLog.Rating);
    }
}
