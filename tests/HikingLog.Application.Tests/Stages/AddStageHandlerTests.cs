namespace HikingLog.Application.Tests.Stages;

using FluentValidation;
using FluentValidation.Results;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Stages.Commands;
using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="AddStageHandler"/>.</summary>
public class AddStageHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly IValidator<AddStage> _validator = Substitute.For<IValidator<AddStage>>();
    private readonly AddStageHandler _handler;

    /// <summary>Initializes a new instance of <see cref="AddStageHandlerTests"/>.</summary>
    public AddStageHandlerTests()
    {
        _handler = new AddStageHandler(_db, _validator);
    }

    /// <summary>When validation fails the handler returns ValidationFailed without touching the database.</summary>
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationFailed()
    {
        var command = new AddStage(1, 1, string.Empty, "Bergen", "Haarlem", 20m, 100m, Difficulty.Easy);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "'Name' must not be empty.")]));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the parent route does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenRouteNotFound_ReturnsNotFound()
    {
        var command = new AddStage(99, 1, "Etappe 1", "Bergen", "Haarlem", 20m, 100m, Difficulty.Easy);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var routes = Substitute.For<DbSet<Route>>();
        // FindAsync returns null — the parent route does not exist.
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>((Route?)null));
        _db.Routes.Returns(routes);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT2);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the command is valid and the parent route exists the handler persists the stage and returns AddStageResult.</summary>
    [Fact]
    public async Task Handle_WhenValid_CallsSaveChangesAndReturnsResult()
    {
        var command = new AddStage(1, 1, "Etappe 1", "Bergen", "Haarlem", 20m, 100m, Difficulty.Easy);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // FindAsync returns an existing route so the parent-exists check passes.
        var existingRoute = new Route { Id = 1, Name = "TestRoute", Code = "TR1", Country = "NL", TotalDistanceKm = 100m };
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>(existingRoute));
        _db.Routes.Returns(routes);

        _db.Stages.Returns(Substitute.For<DbSet<Stage>>());
        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
