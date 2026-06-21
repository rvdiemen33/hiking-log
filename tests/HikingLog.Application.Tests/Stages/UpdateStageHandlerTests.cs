namespace HikingLog.Application.Tests.Stages;

using FluentValidation;
using FluentValidation.Results;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Stages.Commands;
using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="UpdateStageHandler"/>.</summary>
public class UpdateStageHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly IValidator<UpdateStage> _validator = Substitute.For<IValidator<UpdateStage>>();
    private readonly UpdateStageHandler _handler;

    /// <summary>Initializes a new instance of <see cref="UpdateStageHandlerTests"/>.</summary>
    public UpdateStageHandlerTests()
    {
        _handler = new UpdateStageHandler(_db, _validator);
    }

    /// <summary>When validation fails the handler returns ValidationFailed without querying the database.</summary>
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationFailed()
    {
        var command = new UpdateStage(1, 1, 1, string.Empty, "Bergen", "Haarlem", 20m, 100m, Difficulty.Easy);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "'Name' must not be empty.")]));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the stage does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenStageNotFound_ReturnsNotFound()
    {
        var command = new UpdateStage(99, 1, 1, "Etappe 1", "Bergen", "Haarlem", 20m, 100m, Difficulty.Easy);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>((Stage?)null));
        _db.Stages.Returns(stages);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT2);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the stage exists but the referenced parent route does not, the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenRouteNotFound_ReturnsNotFound()
    {
        var command = new UpdateStage(1, 99, 1, "Etappe 1", "Bergen", "Haarlem", 20m, 100m, Difficulty.Easy);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>(new Stage { Id = 1, RouteId = 1, Number = 1, Name = "Oud", StartPoint = "A", EndPoint = "B", DistanceKm = 10m, ElevationDifferenceM = 50m, Difficulty = Difficulty.Easy }));
        _db.Stages.Returns(stages);

        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>((Route?)null));
        _db.Routes.Returns(routes);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT2);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the command is valid and the stage exists the handler updates and returns UpdateStageResult.</summary>
    [Fact]
    public async Task Handle_WhenValid_UpdatesStageAndReturnsResult()
    {
        var existingStage = new Stage
        {
            Id = 1,
            RouteId = 1,
            Number = 1,
            Name = "Oud",
            StartPoint = "A",
            EndPoint = "B",
            DistanceKm = 10m,
            ElevationDifferenceM = 50m,
            Difficulty = Difficulty.Easy
        };
        var command = new UpdateStage(1, 1, 2, "Nieuw", "Bergen", "Haarlem", 25m, 200m, Difficulty.Hard);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var stages = Substitute.For<DbSet<Stage>>();
        stages.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Stage?>(existingStage));
        _db.Stages.Returns(stages);

        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>(new Route { Id = 1, Name = "R", Code = "R1", Country = "NL", TotalDistanceKm = 1m }));
        _db.Routes.Returns(routes);

        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        Assert.Equal("Nieuw", existingStage.Name);
        Assert.Equal(25m, existingStage.DistanceKm);
        Assert.Equal(Difficulty.Hard, existingStage.Difficulty);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
