namespace HikingLog.IntegrationTests.HikeLogs.Fakers;

using Bogus;
using HikingLog.Api.HikeLogs;

/// <summary>Generates valid <see cref="CreateHikeLogRequest"/> test data using Bogus.</summary>
public class HikeLogFaker : Faker<CreateHikeLogRequest>
{
    /// <summary>Initializes a new instance of <see cref="HikeLogFaker"/> with a specific parent stage id.</summary>
    /// <param name="stageId">The primary key of the parent stage.</param>
    public HikeLogFaker(int stageId) : base("nl")
    {
        CustomInstantiator(f => new CreateHikeLogRequest(
            stageId,
            f.Date.PastDateOnly(2),
            f.Random.Int(60, 480),
            f.PickRandom("Sunny", "Cloudy", "Rainy", "Windy", "Foggy"),
            null,
            f.Random.Int(1, 5)));
    }
}
