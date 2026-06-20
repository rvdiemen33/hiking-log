---
name: dotnet-ef-migration
description: >
  This skill should be used whenever Entity Framework Core database migrations need to be
  created, applied, rolled back, removed, or listed in the HikingLog project. Activate when
  the user wants to add a migration after changing a domain entity, DbContext, or Fluent API
  configuration, when the database schema needs to be updated, when a migration must be undone,
  or when migration history needs to be inspected. Also activate when the user mentions EF,
  dotnet ef commands, schema changes, or database updates. This skill only runs the `dotnet ef`
  CLI — it does NOT write entity/DbContext/Fluent code (use domain-entity for that, then run this
  to generate the migration). The slice-builder agent invokes this right after domain-entity.
---

# EF Core Migrations — HikingLog

## Project layout

Migrations live in `HikingLog.Infrastructure`. The startup project is `HikingLog.Api`.
Always pass both `--project` and `--startup-project` flags — without them, EF cannot locate the
connection string or the design-time factory.

## Commands

All commands are single-line for Windows PowerShell compatibility.

### Add a migration

Run after changing an entity, DbContext, or Fluent API configuration. Make sure `dotnet build` passes first.

```powershell
dotnet ef migrations add <Name> --project src/HikingLog.Infrastructure --startup-project src/HikingLog.Api
```

Naming conventions:
- PascalCase, descriptive noun phrases: `AddRouteTable`, `AddRouteNameIndex`, `AddWeatherToHikeLog`
- Name should say what schema change is happening, not what code change triggered it
- Never use vague names like `Update`, `Fix`, or `Migration1`

After adding, review the generated file in `src/HikingLog.Infrastructure/Migrations/` — check that `Up()` creates what you expect and `Down()` reverses it cleanly.

### Apply migrations to the database

```powershell
dotnet ef database update --project src/HikingLog.Infrastructure --startup-project src/HikingLog.Api
```

### List existing migrations

```powershell
dotnet ef migrations list --project src/HikingLog.Infrastructure --startup-project src/HikingLog.Api
```

### Remove the last migration (only when not yet applied)

```powershell
dotnet ef migrations remove --project src/HikingLog.Infrastructure --startup-project src/HikingLog.Api
```

This only works if the migration has **not** been applied to the database. If it has already been applied, add a new corrective migration instead — do not try to remove an applied migration.

### Roll back to a specific migration

```powershell
dotnet ef database update <MigrationName> --project src/HikingLog.Infrastructure --startup-project src/HikingLog.Api
```

## Workflow

1. Change entities, DbContext, or Fluent API configuration in the appropriate project
2. Run `dotnet build` — fix any compilation errors before continuing
3. Add a migration with a descriptive name
4. Review the generated `Up()` and `Down()` methods in `src/HikingLog.Infrastructure/Migrations/`
5. Apply with `dotnet ef database update`
6. Run `dotnet build` and the test suite to confirm nothing broke

## Rules

- Never edit generated migration files manually — remove and regenerate instead
- Always review the generated migration before applying it
- Keep migrations small and focused: one logical schema change per migration
- Never delete or squash applied migrations from a shared or production database
