namespace HikingLog.Application.Tests.Stages;

using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Stages.Queries;
using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="GetStageHandler"/>.</summary>
public class GetStageHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly GetStageHandler _handler;

    /// <summary>Initializes a new instance of <see cref="GetStageHandlerTests"/>.</summary>
    public GetStageHandlerTests()
    {
        _handler = new GetStageHandler(_db);
    }

    /// <summary>When the stage does not exist the handler returns NotFound.</summary>
    [Fact]
    public async Task Handle_WhenStageNotFound_ReturnsNotFound()
    {
        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>((Stage?)null));
        _db.Stages.Returns(stages);

        var result = await _handler.Handle(new GetStage(99), CancellationToken.None);

        Assert.True(result.IsT1);
    }

    /// <summary>When the stage exists the handler returns a StageDto with the correct values.</summary>
    [Fact]
    public async Task Handle_WhenStageExists_ReturnsStageDto()
    {
        var existingStage = new Stage
        {
            Id = 1,
            RouteId = 2,
            Number = 3,
            Name = "Etappe 1",
            StartPoint = "Bergen",
            EndPoint = "Haarlem",
            DistanceKm = 22.5m,
            ElevationDifferenceM = 150m,
            Difficulty = Difficulty.Moderate
        };
        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>(existingStage));
        _db.Stages.Returns(stages);

        var result = await _handler.Handle(new GetStage(1), CancellationToken.None);

        Assert.True(result.IsT0);
        var dto = result.AsT0;
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.RouteId);
        Assert.Equal("Etappe 1", dto.Name);
        Assert.Equal(22.5m, dto.DistanceKm);
        Assert.Equal(Difficulty.Moderate, dto.Difficulty);
    }
}
