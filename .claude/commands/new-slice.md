Scaffold a new vertical slice in the HikingLog project.

If the user did not provide a feature name, ask for one (e.g. "Route", "Etappe", "HikeLog").

Create the following structure (replace `<Feature>` with the feature name):

```
src/HikingLog.Application/<Feature>/
├── Commands/
│   └── Add<Feature>.cs
└── Queries/
    └── Get<Feature>s.cs
```

Use this boilerplate for `Add<Feature>.cs`:

```csharp
namespace HikingLog.Application.<Feature>.Commands;

/// <summary>Command to add a new <feature>.</summary>
public record Add<Feature>();

/// <summary>Result returned after adding a <feature>.</summary>
public record Add<Feature>Result();

internal sealed class Add<Feature>Validator : AbstractValidator<Add<Feature>>
{
    /// <summary>Initializes validation rules for <see cref="Add<Feature>"/>.</summary>
    public Add<Feature>Validator()
    {
    }
}

/// <summary>Handles the <see cref="Add<Feature>"/> command.</summary>
internal sealed class Add<Feature>Handler(IHikingLogDataContext db)
    : ICommandHandler<Add<Feature>, OneOf<Add<Feature>Result, ValidationFailed>>
{
    /// <inheritdoc/>
    public async Task<OneOf<Add<Feature>Result, ValidationFailed>> Handle(
        Add<Feature> command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

Use this boilerplate for `Get<Feature>s.cs`:

```csharp
namespace HikingLog.Application.<Feature>.Queries;

/// <summary>Query to retrieve all <feature>s.</summary>
public record Get<Feature>s();

/// <summary>Single <feature> item in the query result.</summary>
public record <Feature>Dto();

/// <summary>Handles the <see cref="Get<Feature>s"/> query.</summary>
internal sealed class Get<Feature>sHandler(IHikingLogDataContext db)
    : IQueryHandler<Get<Feature>s, IReadOnlyList<<Feature>Dto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<<Feature>Dto>> Handle(
        Get<Feature>s query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

After creating the files:
1. Remind the user to register the handlers manually in `src/HikingLog.Application/Extensions/ServiceCollectionExtensions.cs`.
2. Remind the user to add the corresponding domain entity in `src/HikingLog.Domain/` if it does not exist yet.
3. Run `dotnet build` to verify the new files compile.
