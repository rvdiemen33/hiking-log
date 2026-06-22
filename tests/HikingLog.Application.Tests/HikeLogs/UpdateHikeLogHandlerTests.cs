namespace HikingLog.Application.Tests.HikeLogs;

using FluentValidation;
using FluentValidation.Results;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.HikeLogs.Commands;
using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="UpdateHikeLogHandler"/>.</summary>
public class UpdateHikeLogHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly IValidator<UpdateHikeLog> _validator = Substitute.For<IValidator<UpdateHikeLog>>();
    private readonly UpdateHikeLogHandler _handler;

    /// <summary>Initializes a new instance of <see cref="UpdateHikeLogHandlerTests"/>.</summary>
    public UpdateHikeLogHandlerTests()
    {
        _handler = new UpdateHikeLogHandler(_db, _validator);
    }

    /// <summary>When validation fails the handler returns ValidationFailed without querying the database.</summary>
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationFailed()
    {
        var command = new UpdateHikeLog(1, 1, DateOnly.FromDateTime(DateTime.Today), 120, string.Empty, null, 4);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Weather", "'Weather' must not be empty.")]));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the hike log does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenHikeLogNotFound_ReturnsNotFound()
    {
        var command = new UpdateHikeLog(99, 1, DateOnly.FromDateTime(DateTime.Today), 120, "Sunny", null, 4);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        hikeLogs.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<HikeLog?>((HikeLog?)null));
        _db.HikeLogs.Returns(hikeLogs);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT2);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the hike log exists but the referenced parent stage does not, the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenStageNotFound_ReturnsNotFound()
    {
        var command = new UpdateHikeLog(1, 99, DateOnly.FromDateTime(DateTime.Today), 120, "Sunny", null, 4);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var existingLog = new HikeLog { Id = 1, StageId = 1, DateHiked = DateOnly.FromDateTime(DateTime.Today), DurationMinutes = 60, Weather = "Cloudy", Notes = null, Rating = 3 };
        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        hikeLogs.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<HikeLog?>(existingLog));
        _db.HikeLogs.Returns(hikeLogs);

        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>((Stage?)null));
        _db.Stages.Returns(stages);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT2);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the command is valid and the hike log exists the handler updates and returns UpdateHikeLogResult.</summary>
    [Fact]
    public async Task Handle_WhenValid_UpdatesHikeLogAndReturnsResult()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var existingLog = new HikeLog { Id = 1, StageId = 1, DateHiked = today, DurationMinutes = 60, Weather = "Cloudy", Notes = null, Rating = 3 };
        var command = new UpdateHikeLog(1, 1, today, 180, "Sunny", "Great hike!", 5);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        hikeLogs.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<HikeLog?>(existingLog));
        _db.HikeLogs.Returns(hikeLogs);

        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>(new Stage { Id = 1, RouteId = 1, Number = 1, Name = "Etappe 1", StartPoint = "A", EndPoint = "B", DistanceKm = 10m, ElevationDifferenceM = 50m, Difficulty = Difficulty.Easy }));
        _db.Stages.Returns(stages);

        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        Assert.Equal(existingLog.Id, result.AsT0.Id);
        Assert.Equal(180, existingLog.DurationMinutes);
        Assert.Equal("Sunny", existingLog.Weather);
        Assert.Equal("Great hike!", existingLog.Notes);
        Assert.Equal(5, existingLog.Rating);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
