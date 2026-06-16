namespace HikingLog.IntegrationTests.Routes.Fakers;

using Bogus;
using HikingLog.Api.Routes;

/// <summary>Generates valid <see cref="CreateRouteRequest"/> test data using Bogus.</summary>
public class RouteFaker : Faker<CreateRouteRequest>
{
    /// <summary>Initializes a new instance of <see cref="RouteFaker"/> with Dutch locale and realistic hiking trail data.</summary>
    public RouteFaker() : base("nl")
    {
        CustomInstantiator(f => new CreateRouteRequest(
            f.Address.City() + "pad",
            "LAW " + f.Random.Int(1, 20),
            "Nederland",
            f.Random.Decimal(50, 600),
            f.Lorem.Sentence()));
    }
}
