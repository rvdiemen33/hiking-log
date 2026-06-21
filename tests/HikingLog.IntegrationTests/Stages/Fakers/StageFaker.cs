namespace HikingLog.IntegrationTests.Stages.Fakers;

using Bogus;
using HikingLog.Api.Stages;
using HikingLog.Domain.Enums;

/// <summary>Generates valid <see cref="CreateStageRequest"/> test data using Bogus.</summary>
public class StageFaker : Faker<CreateStageRequest>
{
    /// <summary>Initializes a new instance of <see cref="StageFaker"/> with a specific parent route id.</summary>
    /// <param name="routeId">The primary key of the parent route.</param>
    public StageFaker(int routeId) : base("nl")
    {
        CustomInstantiator(f => new CreateStageRequest(
            routeId,
            f.Random.Int(1, 50),
            f.Address.City() + "etappe",
            f.Address.City(),
            f.Address.City(),
            f.Random.Decimal(5, 30),
            f.Random.Decimal(0, 500),
            f.PickRandom<Difficulty>()));
    }
}
