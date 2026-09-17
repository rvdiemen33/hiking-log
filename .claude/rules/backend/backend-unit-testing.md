---
paths:
  - "tests/HikingLog.Application.Tests/**"
  - "tests/HikingLog.Api.Tests/**"
---

# Backend unit testing

Unit tests verify **isolated logic** — no database, no HTTP, no container. Instantiate the subject directly,
substitute only its dependencies, and assert on the returned `OneOf` **and** on the calls made to the data
context.

## Libraries

xUnit (`[Fact]`; `[Theory]` + `[InlineData]` for several inputs), NSubstitute (never Moq), Bogus for test
data, FluentValidation.TestHelper for validators. No shared mutable state between tests.

## Structure — mirrors `src/` one-to-one

```
tests/HikingLog.Application.Tests/
└── <Feature>/
    ├── <Command>HandlerTests.cs         ← AddRouteHandlerTests, DeleteStageHandlerTests, …
    ├── <Query>HandlerTests.cs           ← GetRouteHandlerTests, …
    └── <Command>ValidatorTests.cs       ← rules + edge cases; one negative test per rule + one happy path
tests/HikingLog.Api.Tests/               ← controller tests: status code, response body, routing — no DB
```

Namespaces mirror the folders (`HikingLog.Application.Tests.<Feature>`). Test class ↔ handler/validator/controller
is one-to-one: every new handler or validator gets its own test class. Test classes and their members carry
XML `<summary>` comments like production code.

## Handler tests

```csharp
public class DeleteRouteHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly DeleteRouteHandler _handler;

    public DeleteRouteHandlerTests() => _handler = new DeleteRouteHandler(_db);

    [Fact]
    public async Task Handle_WhenRouteNotFound_ReturnsNotFound()
    {
        var routes = Substitute.For<DbSet<Route>>();
        routes.FindAsync(Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
              .Returns(new ValueTask<Route?>((Route?)null));
        _db.Routes.Returns(routes);

        var result = await _handler.Handle(new DeleteRoute(99), CancellationToken.None);

        Assert.True(result.IsT1);
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

- Naming: `Handle_When<Condition>_<ExpectedOutcome>` — `Handle_WhenValid_CallsSaveChangesAndReturnsResult`,
  `Handle_WhenValidationFails_ReturnsValidationFailed`, `Handle_WhenRouteNotFound_ReturnsNotFound`.
- Stub the validator in handler tests
  (`_validator.ValidateAsync(command, Arg.Any<CancellationToken>()).Returns(new ValidationResult(...))`) —
  validator *rules* belong in the validator test class, not here.
- Stub the `DbSet` with `Substitute.For<DbSet<T>>()` and stub `FindAsync(Arg.Any<object?[]>(),
  Arg.Any<CancellationToken>())` with a `ValueTask<T?>` for existence checks.
- Assert the `OneOf` arm (`result.IsT0` / `IsT1` / `IsT2`; unwrap with `AsT0`) **and** the side effect
  (`await _db.Received(1).SaveChangesAsync(...)`, `_db.Routes.Received(1).Remove(entity)`, or
  `DidNotReceive()` on failure paths). `Assert.NotNull(result)` alone is not an assertion.
- Always pass `CancellationToken.None`.
- **Minimum per command handler:** valid path, validation-failed path, and — for Update, Delete and child
  Add/Update — the not-found path(s). **Per single-item query:** found and not-found.

## What cannot be unit-tested here

Collection query handlers use `ToListAsync`, which needs an `IAsyncQueryProvider` and throws against a
substituted `DbSet<T>`. Keep at most a construction placeholder test for those handlers and cover the real
filtering, ordering and projection with a Tier 0 endpoint test (and/or a Tier 3 handler test) — see
`backend-integration-testing.md`. Likewise do not unit-test HTTP status codes, routing, or the 400 validation
response: Tier 0 exercises the real pipeline, so a mocked unit test would be redundant.

## Validator tests

`private readonly AddRouteValidator _validator = new();` then `_validator.TestValidate(command)` with
`result.ShouldHaveValidationErrorFor(x => x.Name)` or `result.ShouldNotHaveAnyValidationErrors()`. Use
`[Theory]` + `[InlineData]` for boundary values (e.g. `Rating` 0, 1, 5, 6). Naming:
`Validate_When<Condition>_HasError` / `Validate_When<Condition>_HasNoError`.
