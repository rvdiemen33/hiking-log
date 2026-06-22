namespace HikingLog.Application.Tests.HikeLogs;

using HikingLog.Application.Data.Contracts;
using HikingLog.Application.HikeLogs.Queries;
using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>Unit tests for <see cref="GetHikeLogHandler"/>.</summary>
public class GetHikeLogHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly GetHikeLogHandler _handler;

    /// <summary>Initializes a new instance of <see cref="GetHikeLogHandlerTests"/>.</summary>
    public GetHikeLogHandlerTests()
    {
        _handler = new GetHikeLogHandler(_db);
    }

    /// <summary>When the hike log does not exist the handler returns NotFound.</summary>
    [Fact]
    public async Task Handle_WhenHikeLogNotFound_ReturnsNotFound()
    {
        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        hikeLogs.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<HikeLog?>((HikeLog?)null));
        _db.HikeLogs.Returns(hikeLogs);

        var result = await _handler.Handle(new GetHikeLog(99), CancellationToken.None);

        Assert.True(result.IsT1);
    }

    /// <summary>When the hike log exists the handler returns a HikeLogDto with the correct values.</summary>
    [Fact]
    public async Task Handle_WhenHikeLogExists_ReturnsHikeLogDto()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var existingLog = new HikeLog
        {
            Id = 1,
            StageId = 2,
            DateHiked = today,
            DurationMinutes = 120,
            Weather = "Sunny",
            Notes = "Great view!",
            Rating = 5
        };
        var hikeLogs = Substitute.For<DbSet<HikeLog>>();
        hikeLogs.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<HikeLog?>(existingLog));
        _db.HikeLogs.Returns(hikeLogs);

        var result = await _handler.Handle(new GetHikeLog(1), CancellationToken.None);

        Assert.True(result.IsT0);
        var dto = result.AsT0;
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.StageId);
        Assert.Equal(today, dto.DateHiked);
        Assert.Equal(120, dto.DurationMinutes);
        Assert.Equal("Sunny", dto.Weather);
        Assert.Equal("Great view!", dto.Notes);
        Assert.Equal(5, dto.Rating);
    }
}
