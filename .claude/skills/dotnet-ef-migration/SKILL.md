---
name: dotnet-ef-migration
description: >
  This skill should be used whenever Entity Framework Core database migrations need to be
  created, applied, rolled back, removed, or listed in the HikingLog project. Activate when
  the user wants to add a migration after changing a domain entity, DbContext, or Fluent API
  configuration, when the database schema needs to be updated, when a migration must be undone,
  or when migration history needs to be inspected. Also activate when the user mentions EF,
  dotnet ef commands, schema changes, or database updates.
---

# EF Core Migrations — HikingLog

## Project layout

Migrations live in `HikingLog.Infrastructure`. The startup project is `HikingLog.Api`.
Always pass both `--project` and `--startup-project` flags.

## Commands

### Add a migration

Run after changing an entity, DbContext, or Fluent API configuration:

```bash
dotnet ef migrations add <Name> \
  --project src/HikingLog.Infrastructure \
  --startup-project src/HikingLog.Api
```

Naming conventions:
- PascalCase, descriptive noun phrases: `AddHikeLogTable`, `AddRouteNameIndex`, `AddBeoordelingToHikeLog`
- Never use vague names like `Update`, `Fix`, or `Migration1`

### Apply migrations to the database

```bash
dotnet ef database update \
  --project src/HikingLog.Infrastructure \
  --startup-project src/HikingLog.Api
```

### List existing migrations

```bash
dotnet ef migrations list \
  --project src/HikingLog.Infrastructure \
  --startup-project src/HikingLog.Api
```

### Remove the last migration (only when not yet applied)

```bash
dotnet ef migrations remove \
  --project src/HikingLog.Infrastructure \
  --startup-project src/HikingLog.Api
```

### Roll back to a specific migration

```bash
dotnet ef database update <MigrationName> \
  --project src/HikingLog.Infrastructure \
  --startup-project src/HikingLog.Api
```

## Workflow

1. Change entities or DbContext configuration in the appropriate project
2. Run `dotnet build` — fix any compilation errors before continuing
3. Add a migration with a descriptive name
4. Review the generated `Up()` and `Down()` methods in `src/HikingLog.Infrastructure/Migrations/`
5. Apply with `dotnet ef database update`
6. Run `dotnet build && dotnet test` to confirm nothing broke

## Rules

- Never edit generated migration files manually — remove and regenerate instead
- Always review the generated migration before applying it
- Keep migrations small and focused: one logical schema change per migration
- Never delete or squash applied migrations from a shared or production database
