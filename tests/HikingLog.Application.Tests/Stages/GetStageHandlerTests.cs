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

    /// <summary>When the stage exists the handler returns a StageDto with the correct values, including the note.</summary>
    [Fact]
    public async Task Handle_WhenStageExists_ReturnsStageDto()
    {
        StubFoundStage(StageWithNotes(1, "Let op: veerpont vaart niet in de winter"));

        var result = await _handler.Handle(new GetStage(1), CancellationToken.None);

        Assert.True(result.IsT0);
        var dto = result.AsT0;
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.RouteId);
        Assert.Equal("Etappe 1", dto.Name);
        Assert.Equal(22.5m, dto.DistanceKm);
        Assert.Equal(Difficulty.Moderate, dto.Difficulty);
        Assert.Equal("Let op: veerpont vaart niet in de winter", dto.Notes);
    }

    /// <summary>When the stage has no note the handler returns a StageDto with a null Notes value.</summary>
    [Fact]
    public async Task Handle_WhenStageHasNoNotes_ReturnsNullNotes()
    {
        StubFoundStage(StageWithNotes(8, null));

        var result = await _handler.Handle(new GetStage(8), CancellationToken.None);

        Assert.True(result.IsT0);
        Assert.Null(result.AsT0.Notes);
    }

    /// <summary>Builds the stage the handler looks up, carrying the given id and note.</summary>
    /// <param name="id">The primary key of the stage.</param>
    /// <param name="notes">The note the stage holds, or <see langword="null"/> when it has none.</param>
    /// <returns>A stage with fixed values apart from its id and note.</returns>
    private static Stage StageWithNotes(int id, string? notes) => new()
    {
        Id = id,
        RouteId = 2,
        Number = 3,
        Name = "Etappe 1",
        StartPoint = "Bergen",
        EndPoint = "Haarlem",
        DistanceKm = 22.5m,
        ElevationDifferenceM = 150m,
        Difficulty = Difficulty.Moderate,
        Notes = notes
    };

    /// <summary>Stubs the stage set so FindAsync returns the given stage.</summary>
    /// <param name="existingStage">The stage FindAsync returns.</param>
    private void StubFoundStage(Stage existingStage)
    {
        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>(existingStage));
        _db.Stages.Returns(stages);
    }
}
