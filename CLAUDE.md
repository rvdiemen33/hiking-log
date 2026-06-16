# Hiking Log

## Wat is dit project?

REST API voor het bijhouden van gelopen etappes op lange-afstandswandelroutes (LAW, Pieterpad, GR5).
Gebouwd met Clean Architecture + CQRS in vier projecten binnen één .NET 10 solution.

```
HikingLog.sln
├── src/
│   ├── HikingLog.Domain          → Entities, enums, constants
│   ├── HikingLog.Application     → Commands, queries, validators, IDataContext interface
│   ├── HikingLog.Infrastructure  → DbContext, Fluent API configs, migrations, seed data
│   └── HikingLog.Api             → Controllers, API models, Program.cs
└── tests/
    ├── HikingLog.Application.Tests
    └── HikingLog.Api.Tests
```

## Bouwcommando's

Voer na elke wijziging uit:

```bash
dotnet build
dotnet test tests/HikingLog.Application.Tests
dotnet test tests/HikingLog.Api.Tests
```

Run de API lokaal:

```bash
dotnet run --project src/HikingLog.Api
```

## Stack

- .NET 10 · ASP.NET Core Web API
- Entity Framework Core 10 (Code First, SQL Server)
- Custom CQRS interfaces — `ICommandHandler<TCommand, TResult>` en `IQueryHandler<TQuery, TResult>` gedefinieerd in `HikingLog.Application`
- Riok.Mapperly (source-gen mapping) of manual extension methods
- OneOf (discriminated unions voor success/failure)
- FluentValidation · Swagger / Scalar
- xUnit · NSubstitute · Bogus (tests)

## Architectuurregels

- Domain kent niemand. Application kent Domain. Infrastructure kent Application + Domain. Api kent Application + Infrastructure.
- Voeg nooit een projectreferentie toe die een binnenste laag laat verwijzen naar een buitenste laag.
- Gebruik geen repository pattern — handlers injecteren `IHikingLogDataContext` direct.
- Definieer `IHikingLogDataContext` in `HikingLog.Application`, implementeer in `HikingLog.Infrastructure`.
- Geef nooit entiteiten terug aan de API-grens — gebruik altijd API models met mapping via extension methods.

## CQRS-structuur (Application)

Organiseer per feature, niet per type:

```
Application/
├── Routes/
│   ├── Commands/
│   │   ├── AddRoute.cs        ← record + validator + handler in één file
│   │   └── UpdateRoute.cs
│   └── Queries/
│       ├── GetRoute.cs        ← record + validator + handler in één file
│       └── GetRoutes.cs
├── Etappes/
│   ├── Commands/
│   └── Queries/
├── HikeLogs/
│   ├── Commands/
│   └── Queries/
├── Data/
│   └── Contracts/
│       └── IHikingLogDataContext.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

## Coding standards

- Validators zijn `internal sealed class` in dezelfde file als het command of de query.
- Gebruik `OneOf<TResult, NotFound>` voor queries die mogelijk niets teruggeven.
- Gebruik Riok.Mapperly of static extension methods voor API model mapping — nooit AutoMapper.
- Gebruik altijd FluentValidation voor invoervalidatie.
- Gebruik C# 13 features waar van toepassing (primary constructors, collection expressions).
- Schrijf geen comments tenzij de reden niet uit de code zelf blijkt.
- Registreer DI via `ServiceCollectionExtensions` per laag — niet rechtstreeks in `Program.cs`.
- Registreer handlers handmatig in `ServiceCollectionExtensions` — geen automatische assembly-scanning.

## HTTP-statuscodes

- 200 OK — succesvolle GET, PUT
- 201 Created — succesvolle POST (met `CreatedAtAction`)
- 204 No Content — succesvolle DELETE
- 404 Not Found — resource bestaat niet

## Scope

- Werk uitsluitend binnen deze repository (`C:\github\hiking-log`).
- Voeg geen packages toe zonder expliciete vraag van de gebruiker.
