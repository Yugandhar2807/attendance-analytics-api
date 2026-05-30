# Prompt — Ingestion Strategy

## Context I gave Claude

> I need to support 3 ingestion modes for biometric punch events:
>
>   1. Single REST POST `/api/v1/punches` — one event, structured JSON
>   2. CSV upload `/api/v1/punches/batch` — multipart/form-data with a CSV file
>      (header: `external_ref,punch_at,device_id,direction`)
>   3. PowerShell webhook `/api/v1/punches/webhook` — JSON envelope containing
>      N punches and an optional source batch id
>
> All three converge on a single internal service that:
>   - resolves `external_ref` → `user_id` via `IUserLookup`
>   - deduplicates against existing `(user_id, punch_at, device_id, direction)`
>   - writes new rows in batches of 256
>   - returns `IngestionResult(accepted, rejected, duplicates, errors)`
>
> Constraints:
>   - No throw-on-not-found for individual rows — collect into `errors[]` and continue
>   - The service is per-tenant; it accesses the DB via `ScopedDbContextFactory`
>   - Use `IAsyncEnumerable<IncomingPunch>` as the input shape so streaming uploads work
>   - Test fixtures must be able to drive it without a real DB (use repository abstraction)
>
> Write the interface, the result types, and the core service.

## What Claude produced

(See [`IIngestionStrategy.cs`](../src/Attendance.Application/Ingestion/IIngestionStrategy.cs)
and [`CoreIngestionService.cs`](../src/Attendance.Application/Ingestion/CoreIngestionService.cs))

## What I changed before committing

1. Renamed `IngestPunches` → `IngestAsync` to follow the `Async` convention.
2. Replaced Claude's first attempt at deduplication (per-row `EXISTS` query) with
   a batched range query (`WHERE user_id IN (...) AND punch_at BETWEEN ...`).
   The first attempt would have been O(N) round-trips for an N-row batch — the
   batched version is one round-trip.
3. Added the `rowIndex` parameter to `IngestionError` so the UI can highlight
   the failing row in the original CSV.

## Lesson

The first AI output was correct-but-slow. Always glance at the data-access
shape before committing AI-generated repository code — N+1 is the most common
failure mode.
