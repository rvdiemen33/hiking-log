---
paths:
  - "tests/HikingLog.IntegrationTests/**"
---

# Backend integration testing

## Scope for this project

Hiking-log has no messaging (no Wolverine, no RabbitMQ). Only two tiers are relevant:

| Tier | Question | Speed |
|------|----------|-------|
| 0 — HTTP contract | Does the API return the correct status code, body, and auth response? | Fast |
| 3 — Handler with database | Does the handler process the command correctly and mutate the database? | Fast |

## Project structure

```
tests/HikingLog.IntegrationTests/
├── Configuration/
│   └── CollectionDefinitions.cs     ← xUnit collections per tier
├── Infrastructure/
│   ├── HikingTestWebApplicationFactory.cs   ← factory + SQL Server container
│   └── IntegrationTest.cs                   ← base test class
├── Seeding/
│   └── DataSeederTests.cs           ← covers Infrastructure/Data/DataSeeder
└── <feature>/
    ├── Fakers/                       ← Bogus builders for test data
    ├── Endpoints/                    ← Tier 0 tests
    └── Consumers/                    ← Tier 3 tests
```

## Infrastructure

- **HikingTestWebApplicationFactory** — starts a SQL Server via Testcontainers, overrides the DbContext registration, applies migrations, and initializes Respawn.
- **Respawn** — resets the database between tests (truncate, no container restart).
- **One container per xUnit collection** — shared via `ICollectionFixture<HikingTestWebApplicationFactory>`.
- **`IntegrationTest` base class implements `IAsyncLifetime`** — its `InitializeAsync()` calls `ResetDatabaseAsync()` before each test. Test classes inherit this; do not re-implement `IAsyncLifetime` in them.

## CollectionDefinitions

```csharp
[CollectionDefinition(nameof(HikingLogTier0Collection))]
public class HikingLogTier0Collection : ICollectionFixture<HikingTestWebApplicationFactory> { }
```

Do not add a new collection per feature — use the existing one. Only create a new collection when isolation is needed (e.g. Tier 3 tests that require the DB to be in a specific initial state).

## Tier 0 — HTTP contract

Inherits from `IntegrationTest`. One class per endpoint. Minimum one test per possible status code.

```csharp
[Collection(nameof(HikingLogTier0Collection))]
public class PostRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task PostRoute_WhenValid_Returns201()
    {
        var client = CreateClient(); // CreateAuthenticatedClient("routes:create") once auth is active
        var response = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
    }

    [Fact]
    public async Task PostRoute_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/routes", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }
}
```

Common status codes per HTTP verb (today; authentication is out of scope per the functional-plan, so
the API returns no 401/403 yet):

| Verb | Required covered |
|------|-----------------|
| GET collection | 200 |
| GET single | 200, 404 |
| POST | 201, 400 |
| PUT | 200, 400, 404 |
| DELETE | 204, 404 |

**When JWT auth is added** (out of scope for now): also cover 403 per verb, and 401 (no/invalid/expired
token) once in a separate `Authentication` class — not repeated per endpoint. Until then, do not write
401/403 tests; they cannot pass against an unauthenticated API.

### Behavioral coverage for filters

A status-only test passes even when a filter is inverted or ignored. Every query parameter or by-parent route
(`?year=2024`, `/stages/{stageId}/hikelogs`) needs at least one test that seeds rows on **both** sides of the
filter and asserts both the count and that every returned row satisfies it. A GET collection needs at least one
seeded "returns existing rows" test — an empty-database 200 alone leaves the projection unverified. Seed parent
records over HTTP (`/routes`, then `/stages`) inside the test; there is no shared seed helper on the base class.

### Test method naming

`<Verb><Entity>_When<Condition>_Returns<StatusCode>` for status tests (`PostRoute_WhenValid_Returns201`,
`DeleteStage_WhenNotFound_Returns404`); `<Verb><Entities>_When<Condition>_<Outcome>` for behavioural tests
(`GetHikeLogs_WhenFilteredByYear_ReturnsOnlyThatYear`).

## Tier 3 — Handler with database

Calls the handler directly — resolve it from DI by its `ICommandHandler<,>` / `IQueryHandler<,>` interface (this project has no IMediator) — without HTTP. Verify database mutations via the DbContext.

```csharp
[Collection(nameof(HikingLogTier0Collection))]
public class AddRouteHandlerTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WhenValid_PersistsRoute()
    {
        using var scope = factory.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>>();

        var result = await handler.Handle(new AddRoute("Noordzeepad", "LAW 1", "Nederland", 495m, null), CancellationToken.None);

        Assert.True(result.IsT0);
        var db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();
        var route = await db.Routes.FindAsync(result.AsT0.Id); // AddRouteResult exposes Id, not RouteId
        Assert.NotNull(route);
    }
}
```

When testing a child handler (e.g. `AddStageHandler`), seed the parent by resolving its handler from the same
scope before calling the handler under test. At minimum every feature's Add handler has a Tier 3 test.

## Fakers

Use Bogus for test data. One faker per entity (named after the entity — `RouteFaker`, not `CreateRouteRequestFaker`), in a `Fakers/` folder next to the tests that use it.

**Always use `CustomInstantiator` — never `RuleFor` — for the API request records.** They are positional records with no parameterless constructor; `Faker<T>` calls `Activator.CreateInstance<T>()` internally and throws `MissingMethodException` at runtime if you use `RuleFor`.

```csharp
public class RouteFaker : Faker<CreateRouteRequest>
{
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
```

Fakers for child resources take the parent id in their constructor (`new StageFaker(routeId)`), so generated
requests always reference a valid parent.

## When not to write an integration test

- Validators — test the rules and edge cases as a unit test in `HikingLog.Application.Tests`.
- Pure domain logic (calculations, mappings) — a unit test is faster and more precise.
- Repetition of status codes already covered by Tier 0 — do not repeat as a unit test.
