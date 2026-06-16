---
name: vertical-slice
description: >
  This skill should be used when implementing a COMPLETE vertical slice across ALL FOUR layers
  (Application, Infrastructure, Api, DI) for HikingLog. Activate when the user asks to implement
  or scaffold a full feature (Routes, Etappes, HikeLogs) end-to-end: all CRUD handlers,
  controller, Fluent API config, and DI registration together. Also activate when the user
  asks to add or scaffold any individual layer component (controller, handlers, DI registration,
  models, mapping) for a feature — the skill contains the authoritative patterns for all layers.
  Do NOT activate for single-handler fixes, refactors, or tasks touching only one layer when
  the user explicitly wants a targeted edit only. Supersedes the /new-slice command.
---

# Vertical Slice — HikingLog

A vertical slice spans all four layers: Domain → Application → Infrastructure → Api, plus DI registration.
Follow this guide top-to-bottom for each new feature.

---

## 0. Domain entity

Before any Application or Infrastructure work, ensure the entity exists in `src/HikingLog.Domain/Entities/`.

```csharp
/// <summary>Represents a long-distance hiking trail.</summary>
public class Route
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the full name of the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the route code (e.g. "LAW 1", "GR5").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the country or region.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Gets or sets the total distance in kilometres.</summary>
    public decimal TotalDistanceKm { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the navigation to etappes on this route.</summary>
    public ICollection<Etappe> Etappes { get; set; } = [];
}
```

Navigation properties (both sides of the relationship) must be declared on the entity so Fluent API can configure them in step 2b. Check `.claude/functional-plan.md` for the full domain model and all required properties.

---

## 1. Application layer

One folder per feature under `src/HikingLog.Application/<Feature>/`.

```
Application/
└── Routes/
    ├── Commands/
    │   ├── AddRoute.cs
    │   ├── UpdateRoute.cs
    │   └── DeleteRoute.cs
    └── Queries/
        ├── GetRoutes.cs
        └── GetRoute.cs
```

Each file contains: **record + validator + handler** — all in one file. No separate files per type.

`ValidationFailed` and `NotFound` are defined in `HikingLog.Application` (shared contracts). `OneOf<T0, T1>` comes from the `OneOf` NuGet package.

### Command — Add (POST)

```csharp
/// <summary>Command to create a new route.</summary>
public record AddRoute(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Result returned after a route is created.</summary>
public record AddRouteResult(int Id);

/// <summary>Validates the <see cref="AddRoute"/> command.</summary>
internal sealed class AddRouteValidator : AbstractValidator<AddRoute>
{
    /// <summary>Initializes a new instance of <see cref="AddRouteValidator"/>.</summary>
    public AddRouteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalDistanceKm).GreaterThan(0);
    }
}

/// <summary>Handles the <see cref="AddRoute"/> command.</summary>
public sealed class AddRouteHandler(IHikingLogDataContext db, IValidator<AddRoute> validator)
    : ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>
{
    /// <inheritdoc/>
    public async Task<OneOf<AddRouteResult, ValidationFailed>> Handle(AddRoute command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return new ValidationFailed(validation.Errors);

        var route = new Route
        {
            Name = command.Name,
            Code = command.Code,
            Country = command.Country,
            TotalDistanceKm = command.TotalDistanceKm,
            Description = command.Description
        };
        db.Routes.Add(route);
        await db.SaveChangesAsync(ct);
        return new AddRouteResult(route.Id);
    }
}
```

**OneOf signatures:**
- `Add`: `OneOf<TResult, ValidationFailed>` — for top-level entities (Route)
- `Add` (child with parent FK check): `OneOf<TResult, ValidationFailed, NotFound>` — when the parent must exist (e.g. AddEtappe checks RouteId, AddHikeLog checks EtappeId)
- `Update`: `OneOf<TResult, ValidationFailed, NotFound>`
- `Delete`: `OneOf<Success, NotFound>`
- `Get single`: `OneOf<TResult, NotFound>`
- `Get collection`: `IReadOnlyList<TDto>` (never fails)

### Command — Update (PUT)

```csharp
/// <summary>Command to update an existing route.</summary>
public record UpdateRoute(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Result returned after a route is updated.</summary>
public record UpdateRouteResult(int Id);

/// <summary>Validates the <see cref="UpdateRoute"/> command.</summary>
internal sealed class UpdateRouteValidator : AbstractValidator<UpdateRoute>
{
    /// <summary>Initializes a new instance of <see cref="UpdateRouteValidator"/>.</summary>
    public UpdateRouteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalDistanceKm).GreaterThan(0);
    }
}

/// <summary>Handles the <see cref="UpdateRoute"/> command.</summary>
public sealed class UpdateRouteHandler(IHikingLogDataContext db, IValidator<UpdateRoute> validator)
    : ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<UpdateRouteResult, ValidationFailed, NotFound>> Handle(UpdateRoute command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return new ValidationFailed(validation.Errors);

        var route = await db.Routes.FindAsync([command.Id], ct);
        if (route is null)
            return new NotFound();

        route.Name = command.Name;
        route.Code = command.Code;
        route.Country = command.Country;
        route.TotalDistanceKm = command.TotalDistanceKm;
        route.Description = command.Description;
        await db.SaveChangesAsync(ct);
        return new UpdateRouteResult(route.Id);
    }
}
```

### Command — Delete (DELETE)

Delete does not require a validator — there is nothing to validate beyond the entity's existence, which the handler already checks.

```csharp
/// <summary>Command to delete a route.</summary>
public record DeleteRoute(int Id);

/// <summary>Handles the <see cref="DeleteRoute"/> command.</summary>
public sealed class DeleteRouteHandler(IHikingLogDataContext db)
    : ICommandHandler<DeleteRoute, OneOf<Success, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<Success, NotFound>> Handle(DeleteRoute command, CancellationToken ct)
    {
        var route = await db.Routes.FindAsync([command.Id], ct);
        if (route is null)
            return new NotFound();

        db.Routes.Remove(route);
        await db.SaveChangesAsync(ct);
        return new Success();
    }
}
```

### Query — Get collection (GET all)

```csharp
/// <summary>Query to retrieve all routes.</summary>
public record GetRoutes;

/// <summary>DTO for a route in a collection response.</summary>
public record RouteDto(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Handles the <see cref="GetRoutes"/> query.</summary>
public sealed class GetRoutesHandler(IHikingLogDataContext db)
    : IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<RouteDto>> Handle(GetRoutes query, CancellationToken ct)
        => await db.Routes
            .Select(r => new RouteDto(r.Id, r.Name, r.Code, r.Country, r.TotalDistanceKm, r.Description))
            .ToListAsync(ct);
}
```

### Query — Get single (GET by id)

```csharp
/// <summary>Query to retrieve a single route by id.</summary>
public record GetRoute(int Id);

/// <summary>Handles the <see cref="GetRoute"/> query.</summary>
public sealed class GetRouteHandler(IHikingLogDataContext db)
    : IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<RouteDto, NotFound>> Handle(GetRoute query, CancellationToken ct)
    {
        var route = await db.Routes.FindAsync([query.Id], ct);
        if (route is null)
            return new NotFound();
        return new RouteDto(route.Id, route.Name, route.Code, route.Country, route.TotalDistanceKm, route.Description);
    }
}
```

---

## 2. Infrastructure layer

### 2a. Add DbSet to context and interface

In `src/HikingLog.Infrastructure/Data/HikingLogDbContext.cs`:

```csharp
/// <summary>Gets or sets the routes.</summary>
public DbSet<Route> Routes { get; set; }
```

In `src/HikingLog.Application/Data/Contracts/IHikingLogDataContext.cs`:

```csharp
/// <summary>Gets the routes DbSet.</summary>
DbSet<Route> Routes { get; }
```

Both must be updated — the handler reads from `IHikingLogDataContext`, and EF Core reads/writes through `HikingLogDbContext`.

### 2b. Fluent API configuration

Create `src/HikingLog.Infrastructure/Data/Configurations/<Feature>Configuration.cs`:

```csharp
/// <summary>Configures the <see cref="Route"/> entity.</summary>
internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Country).IsRequired().HasMaxLength(100);
        builder.Property(r => r.TotalDistanceKm).HasColumnType("decimal(10,2)");

        // Navigation: Route (1) → Etappe (n)
        builder.HasMany(r => r.Etappes)
               .WithOne(e => e.Route)
               .HasForeignKey(e => e.RouteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Register it in `OnModelCreating` in `HikingLogDbContext`:

```csharp
modelBuilder.ApplyConfiguration(new RouteConfiguration());
```

**Fluent API conventions:**
- `HasMaxLength` on all `string` columns
- `HasColumnType("decimal(10,2)")` for all `decimal` properties
- `HasConversion<string>()` for enum properties (stored as string, e.g. `Difficulty`)
- Define navigation properties in both directions; declare FK explicitly
- Define each FK relationship in the **child entity's** configuration, not the parent's. `HikeLogConfiguration` owns `HasOne(h => h.Etappe).WithMany(...)`, not `EtappeConfiguration`.

### 2c. Create the migration

After Infrastructure changes are in place, use the **`dotnet-ef-migration` skill** to add the migration. Do not proceed to the API layer before `dotnet build` passes.

---

## 3. API layer

### 3a. Request/response models

Create `src/HikingLog.Api/<Feature>/` with one file for models and one for mapping:

```csharp
// Models.cs
/// <summary>Request body for creating a route.</summary>
public record CreateRouteRequest(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Request body for updating a route.</summary>
public record UpdateRouteRequest(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Response model for a route.</summary>
public record RouteResponse(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);
```

```csharp
// MappingExtensions.cs
/// <summary>Mapping extensions for route models.</summary>
internal static class RouteMappingExtensions
{
    /// <summary>Maps a <see cref="CreateRouteRequest"/> to an <see cref="AddRoute"/> command.</summary>
    internal static AddRoute ToCommand(this CreateRouteRequest r) =>
        new(r.Name, r.Code, r.Country, r.TotalDistanceKm, r.Description);

    /// <summary>Maps an <see cref="UpdateRouteRequest"/> to an <see cref="UpdateRoute"/> command.</summary>
    internal static UpdateRoute ToCommand(this UpdateRouteRequest r, int id) =>
        new(id, r.Name, r.Code, r.Country, r.TotalDistanceKm, r.Description);

    /// <summary>Maps a <see cref="RouteDto"/> to a <see cref="RouteResponse"/>.</summary>
    internal static RouteResponse ToResponse(this RouteDto dto) =>
        new(dto.Id, dto.Name, dto.Code, dto.Country, dto.TotalDistanceKm, dto.Description);
}
```

Note: `UpdateRouteRequest` deliberately omits the `Id` — the controller passes the route segment `{id}` separately as the first argument to `.ToCommand(id)`.

### 3b. Controller

Create `src/HikingLog.Api/<Feature>/<Feature>Controller.cs`:

```csharp
/// <summary>Controller for managing routes.</summary>
[ApiController]
[Route("[controller]")]
public sealed class RoutesController(
    ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> addHandler,
    ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>> updateHandler,
    ICommandHandler<DeleteRoute, OneOf<Success, NotFound>> deleteHandler,
    IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>> getRoutesHandler,
    IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>> getRouteHandler) : ControllerBase
{
    /// <summary>Gets all routes.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RouteResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await getRoutesHandler.Handle(new GetRoutes(), ct);
        return Ok(result.Select(r => r.ToResponse()));
    }

    /// <summary>Gets a route by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await getRouteHandler.Handle(new GetRoute(id), ct);
        return result.Match<IActionResult>(dto => Ok(dto.ToResponse()), _ => NotFound());
    }

    /// <summary>Creates a new route.</summary>
    [HttpPost]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var result = await addHandler.Handle(request.ToCommand(), ct);
        return result.Match<IActionResult>(
            r => CreatedAtAction(nameof(GetById), new { id = r.Id }, r),
            v => ValidationProblem(v.ToModelStateDictionary()));
    }

    /// <summary>Updates an existing route.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRouteRequest request, CancellationToken ct)
    {
        var result = await updateHandler.Handle(request.ToCommand(id), ct);
        return result.Match<IActionResult>(r => Ok(r), v => ValidationProblem(v.ToModelStateDictionary()), _ => NotFound());
    }

    /// <summary>Deletes a route.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await deleteHandler.Handle(new DeleteRoute(id), ct);
        return result.Match<IActionResult>(_ => NoContent(), _ => NotFound());
    }
}
```

**HTTP status code rules:**
- `CreatedAtAction(nameof(GetById), ...)` → 201
- `Ok(...)` → 200
- `NoContent()` → 204
- `NotFound()` → 404
- `ValidationProblem(v.ToModelStateDictionary())` → 400
- Never return entity objects — always use API response models

**`ToModelStateDictionary()` extension method** — add once to `src/HikingLog.Api/Extensions/ValidationFailedExtensions.cs`:

```csharp
/// <summary>Extension methods for converting <see cref="ValidationFailed"/> to ASP.NET Core types.</summary>
internal static class ValidationFailedExtensions
{
    /// <summary>Converts a <see cref="ValidationFailed"/> to a <see cref="ModelStateDictionary"/>.</summary>
    internal static ModelStateDictionary ToModelStateDictionary(this ValidationFailed failed)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in failed.Errors)
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        return modelState;
    }
}
```

**Critical: inject handlers directly — never use IMediator.** This project uses custom `ICommandHandler<T, R>` and `IQueryHandler<T, R>` interfaces.

---

## 4. DI registration

In `src/HikingLog.Application/Extensions/ServiceCollectionExtensions.cs` (create if it does not exist):

```csharp
/// <summary>Registers Application layer services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds Application services to the DI container.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Routes
        services.AddScoped<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>, AddRouteHandler>();
        services.AddScoped<ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>>, UpdateRouteHandler>();
        services.AddScoped<ICommandHandler<DeleteRoute, OneOf<Success, NotFound>>, DeleteRouteHandler>();
        services.AddScoped<IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>>, GetRoutesHandler>();
        services.AddScoped<IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>>, GetRouteHandler>();

        // Validators
        services.AddScoped<IValidator<AddRoute>, AddRouteValidator>();
        services.AddScoped<IValidator<UpdateRoute>, UpdateRouteValidator>();

        return services;
    }
}
```

In `src/HikingLog.Api/Program.cs`, add if missing:

```csharp
builder.Services.AddApplication();
```

This call must sit alongside `builder.Services.AddInfrastructure(builder.Configuration)` — both are required for the application to start.

**Worktree note:** When tasks run in parallel git worktrees, each worktree has its own copy of `ServiceCollectionExtensions.cs` and `Program.cs`. Add only your slice's registrations — merge conflicts will need to be resolved when branches are combined.

---

## Checklist

- [ ] Domain entity created in `src/HikingLog.Domain/Entities/` with navigation properties
- [ ] Application: 5 files (3 commands + 2 queries), each with record + validator (where applicable) + handler
- [ ] Infrastructure: `DbSet` added to `HikingLogDbContext` AND `IHikingLogDataContext`; `IEntityTypeConfiguration` created and registered in `OnModelCreating`; FK relationships configured in the **child's** configuration; navigation properties defined in both directions
- [ ] API: `Models.cs` and `MappingExtensions.cs` (all `internal static`); controller with typed handler injection; `ValidationProblem()` for 400s; `ProducesResponseType` attributes on all actions
- [ ] DI: all handlers and validators registered in `AddApplication()`; `builder.Services.AddApplication()` called in `Program.cs`
- [ ] XML doc `<summary>` on every type and member
- [ ] **Run the `dotnet-ef-migration` skill** to create and apply the migration — this is a required final step, not optional
