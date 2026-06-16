namespace HikingLog.Application.Tests.Routes;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Routes.Queries;
using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="GetRouteHandler"/>.</summary>
public class GetRouteHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly GetRouteHandler _handler;

    /// <summary>Initializes a new instance of <see cref="GetRouteHandlerTests"/>.</summary>
    public GetRouteHandlerTests()
    {
        _handler = new GetRouteHandler(_db);
    }

    /// <summary>When the route does not exist the handler returns NotFound.</summary>
    [Fact]
    public async Task Handle_WhenRouteNotFound_ReturnsNotFound()
    {
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>((Route?)null));
        _db.Routes.Returns(routes);

        var result = await _handler.Handle(new GetRoute(99), CancellationToken.None);

        Assert.True(result.IsT1);
    }

    /// <summary>When the route exists the handler returns a RouteDto with the correct values.</summary>
    [Fact]
    public async Task Handle_WhenRouteExists_ReturnsRouteDto()
    {
        var existingRoute = new Route
        {
            Id = 1,
            Name = "Noordzeepad",
            Code = "LAW 1",
            Country = "Nederland",
            TotalDistanceKm = 495m,
            Description = "Langs de kust"
        };
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>(existingRoute));
        _db.Routes.Returns(routes);

        var result = await _handler.Handle(new GetRoute(1), CancellationToken.None);

        Assert.True(result.IsT0);
        var dto = result.AsT0;
        Assert.Equal(1, dto.Id);
        Assert.Equal("Noordzeepad", dto.Name);
        Assert.Equal("LAW 1", dto.Code);
        Assert.Equal(495m, dto.TotalDistanceKm);
    }
}
