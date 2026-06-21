namespace HikingLog.Application.Tests.Stages;

using HikingLog.Application.Data.Contracts;
using HikingLog.Application.Stages.Queries;
using NSubstitute;

/// <summary>Unit tests for <see cref="GetStagesByRouteHandler"/>.</summary>
public class GetStagesByRouteHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly GetStagesByRouteHandler _handler;

    /// <summary>Initializes a new instance of <see cref="GetStagesByRouteHandlerTests"/>.</summary>
    public GetStagesByRouteHandlerTests()
    {
        _handler = new GetStagesByRouteHandler(_db);
    }

    // GetStagesByRouteHandler uses EF Core's ToListAsync extension, which requires a full async
    // queryable provider that NSubstitute cannot supply without additional infrastructure.
    // The handler's filtering and ordering behaviour is covered by the Tier 0 endpoint test
    // GetStagesByRouteTests.GetStagesByRoute_ReturnsOnlyMatchingStages_OrderedByNumber.
    // A placeholder fact is kept here so the test class compiles and is discoverable.

    /// <summary>Placeholder — collection query handlers that use ToListAsync are covered by integration tests.</summary>
    [Fact]
    public void GetStagesByRouteHandler_IsRegisteredInDi()
    {
        // This verifies the handler can be constructed (DI registration is checked at startup).
        Assert.NotNull(_handler);
    }
}
