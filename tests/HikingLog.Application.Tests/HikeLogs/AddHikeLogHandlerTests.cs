namespace HikingLog.Application.Tests.HikeLogs;

using FluentValidation;
using FluentValidation.Results;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.HikeLogs.Commands;
using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="AddHikeLogHandler"/>.</summary>
public class AddHikeLogHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly IValidator<AddHikeLog> _validator = Substitute.For<IValidator<AddHikeLog>>();
    private readonly AddHikeLogHandler _handler;

    /// <summary>Initializes a new instance of <see cref="AddHikeLogHandlerTests"/>.</summary>
    public AddHikeLogHandlerTests()
    {
        _handler = new AddHikeLogHandler(_db, _validator);
    }

    /// <summary>When validation fails the handler returns ValidationFailed without touching the database.</summary>
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationFailed()
    {
        var command = new AddHikeLog(1, DateOnly.FromDateTime(DateTime.Today), 120, string.Empty, null, 4);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Weather", "'Weather' must not be empty.")]));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the parent stage does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenStageNotFound_ReturnsNotFound()
    {
        var command = new AddHikeLog(99, DateOnly.FromDateTime(DateTime.Today), 120, "Sunny", null, 4);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var stages = Substitute.For<DbSet<Stage>>();
        // FindAsync returns null — the parent stage does not exist.
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>((Stage?)null));
        _db.Stages.Returns(stages);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT2);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the command is valid and the parent stage exists the handler persists the hike log and returns AddHikeLogResult.</summary>
    [Fact]
    public async Task Handle_WhenValid_CallsSaveChangesAndReturnsResult()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var command = new AddHikeLog(1, today, 120, "Sunny", null, 4);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // FindAsync returns an existing stage so the parent-exists check passes.
        var existingStage = new Stage { Id = 1, RouteId = 1, Number = 1, Name = "Etappe 1", StartPoint = "A", EndPoint = "B", DistanceKm = 10m, ElevationDifferenceM = 50m, Difficulty = Domain.Enums.Difficulty.Easy };
        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>(existingStage));
        _db.Stages.Returns(stages);

        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        _db.HikeLogs.Returns(hikeLogs);
        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        hikeLogs.Received(1).Add(Arg.Is<HikeLog>(h => h.StageId == 1 && h.Weather == "Sunny" && h.Rating == 4));
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
