# Architecture

```
                                                  ┌─────────────────────┐
                                                  │  Power BI / Web UI  │
                                                  └─────────┬───────────┘
                                                            │ HTTPS
                                                            ▼
                              ┌──────────────────────────────────────────────────┐
                              │             ASP.NET Core 8 (this repo)            │
                              │                                                  │
                              │   Middleware: TenantResolution                    │
                              │                                                  │
                              │   Endpoints (3 ingestion modes + analytics):     │
                              │     POST /api/v1/punches          [Mode 1 — REST] │
                              │     POST /api/v1/punches/batch    [Mode 2 — CSV ] │
                              │     POST /api/v1/punches/webhook  [Mode 3 — PS  ] │
                              │     GET  /api/v1/analytics/...                   │
                              │     POST /api/v1/ai/infer-schema  [AI-assisted]  │
                              │                                                  │
                              │   ScopedDbContextFactory → AppDbContext          │
                              │                                                  │
                              └─────┬──────────────────────────┬─────────────────┘
                                    │                          │
                       per-tenant SQL                       optional
                                    │                          │
                                    ▼                          ▼
                  ┌────────────────────────────┐    ┌──────────────────┐
                  │ SQL Server (DB-per-tenant) │    │  Anthropic API   │
                  │   AttendanceTenantA        │    │  (Claude)        │
                  │   AttendanceTenantB        │    └──────────────────┘
                  │   AttendanceTenantC ...    │
                  └────────────────────────────┘
                            ▲
                            │  (mode 3 webhook)
                  ┌─────────┴──────────┐
                  │ PowerShell script  │
                  │ on building PC     │
                  └────────────────────┘
```

## Clean Architecture layout

Solution has 4 source projects + 2 test projects + 1 seeder.

```
src/
├── Attendance.Domain/         ← entities, value objects, NO dependencies
├── Attendance.Application/    ← use cases, interfaces (no I/O)
├── Attendance.Infrastructure/ ← EF Core, HttpClient (Claude), tenant resolver
└── Attendance.Api/            ← HTTP layer (endpoints, middleware, Program.cs)

tests/
├── Attendance.UnitTests/      ← Domain + Application
└── Attendance.IntegrationTests/  ← uses WebApplicationFactory<Program>

tools/SeedData/                ← console app: synthetic data generator (Bogus)
```

Each layer references only the one below it:

```
Api ──▶ Application ──▶ Domain
 │
 └────▶ Infrastructure ──▶ Application ──▶ Domain
```

## Request lifecycle (typical /api/v1/punches POST)

1. Kestrel accepts the HTTP request
2. **Serilog request logging** starts the activity span
3. **TenantResolutionMiddleware** reads `X-Tenant-Id` → `TenantId` value object → ITenantResolver lookup → stores on scoped `TenantContext`
4. Endpoint handler runs with DI-injected `IIngestionStrategy`
5. `CoreIngestionService` maps incoming punches → users (via `IUserLookup`)
6. `EfPunchRepository.InsertSkipDuplicatesAsync` is called with batches of 256
7. `ScopedDbContextFactory.CreateAsync` creates a per-request AppDbContext bound to **this tenant's** connection string
8. EF Core writes batch → SQL Server (per-tenant DB)
9. Result envelope returned, request logged with duration + tenant id

## Why "minimal APIs" over MVC controllers

| | Minimal APIs | Controllers (MVC) |
|---|---|---|
| Boilerplate | Low | Higher |
| Familiarity | Newer (.NET 6+) | Familiar to most teams |
| Performance | Slightly faster | Slightly slower |
| Testability | Same (uses WebApplicationFactory) | Same |
| OpenAPI metadata | Verbose `.With*()` chains | Attribute-based |
| **Verdict here** | **Wins for the per-endpoint clarity in a showcase** | Better if you're maintaining a 200-endpoint API |
