namespace HikingLog.Application.Tests.Routes;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Routes.Commands;
using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="DeleteRouteHandler"/>.</summary>
public class DeleteRouteHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly DeleteRouteHandler _handler;

    /// <summary>Initializes a new instance of <see cref="DeleteRouteHandlerTests"/>.</summary>
    public DeleteRouteHandlerTests()
    {
        _handler = new DeleteRouteHandler(_db);
    }

    /// <summary>When the route does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenRouteNotFound_ReturnsNotFound()
    {
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>((Route?)null));
        _db.Routes.Returns(routes);

        var result = await _handler.Handle(new DeleteRoute(99), CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the route exists the handler removes it and returns Success.</summary>
    [Fact]
    public async Task Handle_WhenRouteExists_RemovesAndReturnsSuccess()
    {
        var existingRoute = new Route { Id = 1, Name = "Noordzeepad", Code = "LAW 1", Country = "Nederland", TotalDistanceKm = 495m };
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>(existingRoute));
        _db.Routes.Returns(routes);
        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new DeleteRoute(1), CancellationToken.None);

        Assert.True(result.IsT0);
        _db.Routes.Received(1).Remove(existingRoute);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
