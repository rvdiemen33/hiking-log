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

xUnit (`[Fact]`; `[Theory]` + `[InlineData]` for several inputs) and NSubstitute (never Moq). That is all
these two projects reference — **Bogus is not available here** (only `HikingLog.IntegrationTests` references
it), so build unit-test data with plain object initialisers. Do not add a package to change that without an
explicit request from the user. No shared mutable state between tests.

## Structure — mirrors `src/` one-to-one

```
tests/HikingLog.Application.Tests/
└── <Feature>/
    ├── <Command>HandlerTests.cs         ← AddRouteHandlerTests, DeleteStageHandlerTests, …
    └── <Query>HandlerTests.cs           ← GetRouteHandlerTests, …
tests/HikingLog.Api.Tests/               ← non-HTTP controller-layer logic: mapping extensions, Match-arm
                                            selection. Status codes, routing and the 400 body belong to
                                            Tier 0, not here. Currently a placeholder only.
```

Namespaces mirror the folders (`HikingLog.Application.Tests.<Feature>`). Test class ↔ handler/validator/controller
is one-to-one: every new handler or validator gets its own test class. Test classes and their members carry
XML `<summary>` comments like production code.

## Handler tests

```csharp
/// <summary>Unit tests for <see cref="DeleteRouteHandler"/>.</summary>
public class DeleteRouteHandlerTests
{
    private readonly IHikingLogDataContext _db = Substitute.For<IHikingLogDataContext>();
    private readonly DeleteRouteHandler _handler;

    /// <summary>Initializes a new instance of <see cref="DeleteRouteHandlerTests"/>.</summary>
    public DeleteRouteHandlerTests()
    {
        _handler = new DeleteRouteHandler(_db);
    }

    /// <summary>When the route does not exist the handler returns NotFound without saving.</summary>
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
- Naming for the validation-failed case: `Handle_WhenValidationFails_ReturnsValidationFailed`.

## What cannot be unit-tested here

Collection query handlers use `ToListAsync`, which needs an `IAsyncQueryProvider` and throws against a
substituted `DbSet<T>`. Keep at most a construction placeholder test for those handlers and cover the real
filtering, ordering and projection with a Tier 0 endpoint test (and/or a Tier 3 handler test) — see
`backend-integration-testing.md`. Likewise do not unit-test HTTP status codes, routing, or the 400 validation
response: Tier 0 exercises the real pipeline, so a mocked unit test would be redundant. And see above —
validators themselves are `internal` and out of reach from the test assembly.

## Validators are not directly testable today

Validators are `internal sealed class` in the command file, and **no project grants `InternalsVisibleTo`
to `HikingLog.Application.Tests`**. A `<Command>ValidatorTests` class that does
`private readonly AddRouteValidator _validator = new();` fails to compile with CS0122, so do not write one
and do not report a missing one as a gap.

Cover validation two ways instead:
- **Unit**: the handler's validation-failed path, with the substituted `IValidator<T>` returning a
  `ValidationResult` that carries errors. This proves the handler short-circuits before touching the
  database, which is the handler's half of the contract.
- **Tier 0**: a 400 test per write endpoint exercises the real validator through the pipeline. Boundary
  values for a specific rule (`Rating` 0, 1, 5, 6) belong there too.

To make validators directly unit-testable, someone must first add
`[assembly: InternalsVisibleTo("HikingLog.Application.Tests")]` to `HikingLog.Application` and reference
FluentValidation.TestHelper from the test project. Both are changes the user has to approve; until then the
two routes above are the whole story.
