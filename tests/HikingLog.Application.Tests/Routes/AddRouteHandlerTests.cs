namespace HikingLog.Application.Tests.Routes;

using FluentValidation;
using FluentValidation.Results;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Routes.Commands;
using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OneOf;

/// <summary>Unit tests for <see cref="AddRouteHandler"/>.</summary>
public class AddRouteHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly IValidator<AddRoute> _validator = Substitute.For<IValidator<AddRoute>>();
    private readonly AddRouteHandler _handler;

    /// <summary>Initializes a new instance of <see cref="AddRouteHandlerTests"/>.</summary>
    public AddRouteHandlerTests()
    {
        _handler = new AddRouteHandler(_db, _validator);
    }

    /// <summary>When validation fails the handler returns ValidationFailed without touching the database.</summary>
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationFailed()
    {
        var command = new AddRoute(string.Empty, "LAW 1", "Nederland", 495m, null);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "'Name' must not be empty.")]));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the command is valid the handler persists the route and returns an AddRouteResult.</summary>
    [Fact]
    public async Task Handle_WhenValid_CallsSaveChangesAndReturnsResult()
    {
        var command = new AddRoute("Noordzeepad", "LAW 1", "Nederland", 495m, null);
        _validator
            .ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _db.Routes.Returns(Substitute.For<DbSet<Route>>());
        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
