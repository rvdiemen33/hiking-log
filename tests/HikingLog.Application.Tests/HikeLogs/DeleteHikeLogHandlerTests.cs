namespace HikingLog.Application.Tests.HikeLogs;

using HikingLog.Application.Data.Contracts;
using HikingLog.Application.HikeLogs.Commands;
using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="DeleteHikeLogHandler"/>.</summary>
public class DeleteHikeLogHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly DeleteHikeLogHandler _handler;

    /// <summary>Initializes a new instance of <see cref="DeleteHikeLogHandlerTests"/>.</summary>
    public DeleteHikeLogHandlerTests()
    {
        _handler = new DeleteHikeLogHandler(_db);
    }

    /// <summary>When the hike log does not exist the handler returns NotFound without saving.</summary>
    [Fact]
    public async Task Handle_WhenHikeLogNotFound_ReturnsNotFound()
    {
        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        hikeLogs.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<HikeLog?>((HikeLog?)null));
        _db.HikeLogs.Returns(hikeLogs);

        var result = await _handler.Handle(new DeleteHikeLog(99), CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the hike log exists the handler removes it and returns Success.</summary>
    [Fact]
    public async Task Handle_WhenHikeLogExists_RemovesAndReturnsSuccess()
    {
        var existingLog = new HikeLog
        {
            Id = 1,
            StageId = 1,
            DateHiked = DateOnly.FromDateTime(DateTime.Today),
            DurationMinutes = 120,
            Weather = "Sunny",
            Notes = null,
            Rating = 4
        };
        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        hikeLogs.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<HikeLog?>(existingLog));
        _db.HikeLogs.Returns(hikeLogs);
        _db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new DeleteHikeLog(1), CancellationToken.None);

        Assert.True(result.IsT0);
        _db.HikeLogs.Received(1).Remove(existingLog);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
