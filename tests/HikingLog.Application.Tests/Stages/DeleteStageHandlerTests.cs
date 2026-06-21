namespace HikingLog.Application.Tests.Stages;

using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Stages.Commands;
using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="DeleteStageHandler"/>.</summary>
public class DeleteStageHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly DeleteStageHandler _handler;

    /// <summary>Initializes a new instance of <see cref="DeleteStageHandlerTests"/>.</summary>
    public DeleteStageHandlerTests()
    {
        _handler = new DeleteStageHandler(_db);
    }

    /// <summary>When the stage does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenStageNotFound_ReturnsNotFound()
    {
        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>((Stage?)null));
        _db.Stages.Returns(stages);

        var result = await _handler.Handle(new DeleteStage(99), CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the stage exists the handler removes it and returns Success.</summary>
    [Fact]
    public async Task Handle_WhenStageExists_RemovesAndReturnsSuccess()
    {
        var existingStage = new Stage
        {
            Id = 1,
            RouteId = 1,
            Number = 1,
            Name = "Etappe 1",
            StartPoint = "Bergen",
            EndPoint = "Haarlem",
            DistanceKm = 20m,
            ElevationDifferenceM = 100m,
            Difficulty = Difficulty.Easy
        };
        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>(existingStage));
        _db.Stages.Returns(stages);
        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new DeleteStage(1), CancellationToken.None);

        Assert.True(result.IsT0);
        _db.Stages.Received(1).Remove(existingStage);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
