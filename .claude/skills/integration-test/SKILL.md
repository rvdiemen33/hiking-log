---
name: integration-test
description: >
  This skill should be used when writing integration tests for HikingLog: Tier 0 (HTTP contract)
  and Tier 3 (handler with database) tests. Activate when the user asks to write or add integration
  tests, mentions Tier 0, Tier 3, HTTP contract tests, Testcontainers, Respawn, or
  HikingLog.IntegrationTests. Also activate for Bogus fakers specifically in the integration test
  project — NOT for unit tests in HikingLog.Application.Tests or HikingLog.Api.Tests.
  Activate proactively any time a new endpoint or handler is implemented and integration test
  coverage does not yet exist for it.
---

# Integration Tests — HikingLog

## Scope

Only Tier 0 and Tier 3 are relevant for this project (no messaging).

| Tier | Scope | Question answered |
|------|-------|-------------------|
| 0 | HTTP contract | Correct status code, body, and content-type? |
| 3 | Handler + database | Does the handler persist/mutate data correctly? |

---

## Project structure

```
tests/HikingLog.IntegrationTests/
├── Configuration/
│   └── CollectionDefinitions.cs
├── Infrastructure/
│   ├── HikingTestWebApplicationFactory.cs
│   └── IntegrationTest.cs
└── <Feature>/
    ├── Fakers/        ← Bogus builders
    ├── Endpoints/     ← Tier 0 tests
    └── Consumers/     ← Tier 3 tests
```

---

## How the test infrastructure works

`HikingTestWebApplicationFactory` starts a SQL Server container via Testcontainers, overrides the DbContext registration, applies migrations, and initializes Respawn. One container is shared across the entire xUnit collection.

`IntegrationTest` is the base class. It calls `ResetDatabaseAsync()` in `InitializeAsync()`, so the database is truncated before every test. You never need to clean up manually — and you never need to re-implement `IAsyncLifetime` in test classes.

---

## Class boilerplate

**Use the existing collection — do NOT create a new collection per feature.**

```csharp
[Collection(nameof(HikingLogTier0Collection))]
public class PostRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    // tests here
}
```

- `[Collection(nameof(HikingLogTier0Collection))]` — exact spelling; wrong name = silent fixture failure
- Constructor signature must match `IntegrationTest(HikingTestWebApplicationFactory factory)`
- `IAsyncLifetime` is implemented in `IntegrationTest` — do **not** re-implement it in test classes

One class per endpoint. Naming convention: `<Verb><Feature>Tests` — e.g. `PostRouteTests`, `GetRouteTests`, `DeleteStageTests`.

---

## Tier 0 — HTTP contract tests

### Required status code coverage

| Verb | Required codes | Notes |
|------|---------------|-------|
| GET collection | 200 | 403 once auth is active |
| GET single | 200, 404 | 403 once auth is active |
| POST | 201, 400 | 403 once auth is active |
| PUT | 200, 400, 404 | 403 once auth is active |
| DELETE | 204, 404 | 403 once auth is active |

401 tests (no token, expired token) go in a separate `Authentication` class — not per endpoint.

### Seeding dependent records

When testing an endpoint for a child resource (Stage, HikeLog), you must first seed the parent record via HTTP before exercising the endpoint under test. Use `CreateClient()` for the seeding call too — Respawn resets between tests so there is no shared state to rely on.

```csharp
// Seed a route first, then create a stage under it
var client = CreateClient();
var routeResponse = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
var routeId = (await routeResponse.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

var response = await client.PostAsJsonAsync("/stages", new StageFaker(routeId).Generate());
Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
```

Pass the parent id into the child faker's constructor so the generated request always references a valid parent.

### POST example

```csharp
[Collection(nameof(HikingLogTier0Collection))]
public class PostRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task PostRoute_WhenValid_Returns201()
    {
        var client = CreateClient();
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

### GET example

```csharp
[Collection(nameof(HikingLogTier0Collection))]
public class GetRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task GetRoute_WhenExists_Returns200()
    {
        var client = CreateClient();
        var created = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var location = created.Headers.Location!;

        var response = await client.GetAsync(location);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task GetRoute_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/routes/99999");
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
```

### PUT example

```csharp
[Collection(nameof(HikingLogTier0Collection))]
public class PutRouteTests(HikingTestWebApplicationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task PutRoute_WhenValid_Returns200()
    {
        var client = CreateClient();
        var created = await client.PostAsJsonAsync("/routes", new RouteFaker().Generate());
        var id = (await created.Content.ReadFromJsonAsync<RouteResponse>())!.Id;

        var response = await client.PutAsJsonAsync($"/routes/{id}", new RouteFaker().Generate());
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task PutRoute_WhenInvalid_Returns400()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/routes/1", new { });
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task PutRoute_WhenNotFound_Returns404()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/routes/99999", new RouteFaker().Generate());
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
```

---

## Tier 3 — Handler with database

Resolve the handler directly from DI — do NOT use the HTTP client.

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

        var command = new AddRoute("Noordzeepad", "LAW 1", "Nederland", 495m, null);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsT0);
        var db = scope.ServiceProvider.GetRequiredService<HikingLogDbContext>();
        var route = await db.Routes.FindAsync(result.AsT0.Id);
        Assert.NotNull(route);
        Assert.Equal("Noordzeepad", route.Name);
    }
}
```

When testing a child handler (e.g. `AddStageHandler`), seed the parent record by resolving its handler from the same scope before calling the handler under test.

---

## Fakers

One faker per entity in `Fakers/`. Use Bogus.

**Naming:** Name the faker after the **entity**, not the request type — `RouteFaker`, not `CreateRouteRequestFaker`. The class name appears in test code dozens of times; the entity name is already familiar.

**Always use `CustomInstantiator` — never `RuleFor` — for C# records.** API request models are positional records and have no parameterless constructor. `Faker<T>` calls `Activator.CreateInstance<T>()` internally and throws `MissingMethodException` at runtime when no parameterless constructor exists. `RuleFor` only works for classes with a parameterless constructor.

```csharp
/// <summary>Generates valid <see cref="CreateRouteRequest"/> test data.</summary>
public class RouteFaker : Faker<CreateRouteRequest>
{
    /// <summary>Initializes a new instance of <see cref="RouteFaker"/>.</summary>
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

For fakers of child resources, accept the parent id in the constructor:

```csharp
/// <summary>Generates valid <see cref="CreateStageRequest"/> test data.</summary>
public class StageFaker : Faker<CreateStageRequest>
{
    /// <summary>Initializes a new instance of <see cref="StageFaker"/>.</summary>
    public StageFaker(int routeId) : base("nl")
    {
        CustomInstantiator(f => new CreateStageRequest(
            routeId,
            f.Random.Int(1, 30),
            f.Address.City() + " - " + f.Address.City(),
            f.Address.City(),
            f.Address.City(),
            f.Random.Decimal(5, 35),
            f.Random.Decimal(0, 800),
            f.PickRandom<Difficulty>()));
    }
}
```

- Pass `"nl"` as the locale to the base constructor for Dutch city/street names
- Place fakers in `Fakers/` next to the test classes that use them

---

## When NOT to write an integration test

- **Validators** — unit test in `HikingLog.Application.Tests` (faster, more precise)
- **Mapping logic** — unit test
- **Status codes already covered in Tier 0** — do not duplicate as a unit test
- **Pure domain calculations** — unit test

---

## Checklist

- [ ] One test class per endpoint (`<Verb><Feature>Tests.cs`)
- [ ] `[Collection(nameof(HikingLogTier0Collection))]` attribute present
- [ ] All required status codes covered per verb (see table above)
- [ ] Faker uses `CustomInstantiator` (records have no parameterless constructor — `RuleFor` throws at runtime)
- [ ] Faker created in `Fakers/` folder; child fakers accept parent id in constructor
- [ ] Tier 0 seeds parent records before testing child endpoints
- [ ] Tier 3 uses `factory.Services.CreateScope()`, not HTTP client
- [ ] `IAsyncLifetime` NOT re-implemented in test class
- [ ] XML doc `<summary>` on fakers and their constructors
- [ ] **Run `dotnet test tests/HikingLog.IntegrationTests` after writing tests** — always verify before reporting done
