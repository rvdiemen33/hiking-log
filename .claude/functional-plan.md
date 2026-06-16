# Functional plan — HikingLog

A REST API for tracking completed stages on long-distance hiking trails.

---

## Domain model

### Route
Represents a long-distance hiking trail (e.g. LAW 1, Pieterpad, GR5).

| Property | Type | Notes |
|----------|------|-------|
| Id | int | PK |
| Name | string | Full name |
| Code | string | Abbreviation, e.g. "LAW 1", "GR5" |
| Country | string | Country or region |
| TotalDistanceKm | decimal | Total length in km |
| Description | string? | Optional |

### Etappe
A single day-stage of a route.

| Property | Type | Notes |
|----------|------|-------|
| Id | int | PK |
| RouteId | int | FK → Route |
| Number | int | Sequence number within the route |
| Name | string | Stage name |
| StartPoint | string | Name of the start location |
| EndPoint | string | Name of the end location |
| DistanceKm | decimal | Length in km |
| ElevationDifferenceM | decimal | Elevation difference in metres |
| Difficulty | enum | Easy / Moderate / Hard |

### HikeLog
A log entry for a completed stage.

| Property | Type | Notes |
|----------|------|-------|
| Id | int | PK |
| EtappeId | int | FK → Etappe |
| DateHiked | DateOnly | Date of the hike |
| DurationMinutes | int | Duration in minutes |
| Weather | string | Weather conditions |
| Notes | string? | Personal note, optional |
| Rating | int | Score 1–5 |

### Relationships

```
Route (1) ──── (n) Etappe (1) ──── (n) HikeLog
```

### Enums

```
Difficulty: Easy | Moderate | Hard
```

---

## API endpoints

### Routes

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/routes` | Get all routes |
| GET | `/routes/{id}` | Get a single route |
| POST | `/routes` | Create a route |
| PUT | `/routes/{id}` | Update a route |
| DELETE | `/routes/{id}` | Delete a route |

### Etappes

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/routes/{routeId}/etappes` | Get all stages for a route |
| GET | `/etappes/{id}` | Get a single stage |
| POST | `/etappes` | Create a stage |
| PUT | `/etappes/{id}` | Update a stage |
| DELETE | `/etappes/{id}` | Delete a stage |

### HikeLogs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/hikelogs` | Get all logs; supports `?year=2024` filter |
| GET | `/hikelogs/{id}` | Get a single log |
| GET | `/etappes/{etappeId}/hikelogs` | Get logs for a specific stage |
| POST | `/hikelogs` | Log a completed stage |
| PUT | `/hikelogs/{id}` | Update a log |
| DELETE | `/hikelogs/{id}` | Delete a log |

### Statistics

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/routes/{id}/progress` | Progress for a single route |
| GET | `/statistics` | Summary across all routes |

**`/routes/{id}/progress` response:**
- Number of completed stages vs. total
- Percentage completed
- Total km hiked
- Total time in hours

**`/statistics` response:**
- Total km across all routes
- Number of distinct routes with at least one log
- Most recent hike date

---

## Business rules

- Validate that the referenced route exists when creating an etappe.
- Validate that the referenced etappe exists when creating a hikelog.
- Rating must be between 1 and 5.

---

## Seed data

Seed three real long-distance routes, each with at least five stages with realistic data:

| Route | Code |
|-------|------|
| Noordzeepad | LAW 1 |
| Pieterpad | LAW 9 |
| GR 5 (Nederland–Monaco) | GR 5 |

---

## Possible extensions (out of scope for now)

- Photo URL on `HikeLog`
- GPX file link per etappe
- Multi-user support (JWT authentication)
- Export to CSV / Excel
- Import from existing Excel file
