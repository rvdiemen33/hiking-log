namespace HikingLog.Application.Tests.Routes;

using FluentValidation;
using FluentValidation.Results;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Routes.Commands;
using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="UpdateRouteHandler"/>.</summary>
public class UpdateRouteHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly IValidator<UpdateRoute> _validator = Substitute.For<IValidator<UpdateRoute>>();
    private readonly UpdateRouteHandler _handler;

    /// <summary>Initializes a new instance of <see cref="UpdateRouteHandlerTests"/>.</summary>
    public UpdateRouteHandlerTests()
    {
        _handler = new UpdateRouteHandler(_db, _validator);
    }

    /// <summary>When validation fails the handler returns ValidationFailed without querying the database.</summary>
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationFailed()
    {
        var command = new UpdateRoute(1, string.Empty, "LAW 1", "Nederland", 495m, null);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "'Name' must not be empty.")]));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the route does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenRouteNotFound_ReturnsNotFound()
    {
        var command = new UpdateRoute(99, "Noordzeepad", "LAW 1", "Nederland", 495m, null);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>((Route?)null));
        _db.Routes.Returns(routes);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT2);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the command is valid and the route exists the handler updates and returns UpdateRouteResult.</summary>
    [Fact]
    public async Task Handle_WhenValid_UpdatesRouteAndReturnsResult()
    {
        var existingRoute = new Route { Id = 1, Name = "Oud", Code = "LAW 1", Country = "Nederland", TotalDistanceKm = 100m };
        var command = new UpdateRoute(1, "Noordzeepad", "LAW 1", "Nederland", 495m, "Beschrijving");
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>(existingRoute));
        _db.Routes.Returns(routes);
        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        Assert.Equal("Noordzeepad", existingRoute.Name);
        Assert.Equal(495m, existingRoute.TotalDistanceKm);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
