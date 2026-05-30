# Showcase Project 01 — `attendance-analytics-api`

> **Build first.** Closest to your day job, highest ROI per hour of effort.

---

## One-liner

Production-pattern ASP.NET Core REST API for institutional attendance tracking with biometric device ingestion, daily roll-up, and KPI endpoints.

## Why this project (recruiter angle)

- Demonstrates **.NET backend skill** — your strongest stack
- Demonstrates **enterprise data modeling** — real schemas, not CRUD-on-Person
- Shows you understand **ingestion → transformation → analytics** flow
- Maps directly to your day-to-day work without leaking employer IP

---

## Architecture

```
   ┌─────────────────┐
   │  Biometric CSV  │
   │   /  Webhook    │
   └────────┬────────┘
            │ POST /api/v1/attendance/punch
            ▼
   ┌────────────────────────────────┐
   │   Ingestion endpoint           │
   │   (validate, queue)            │
   └────────────────────────────────┘
            │
            ▼
   ┌────────────────────────────────┐
   │   Background worker            │
   │   (Hangfire / IHostedService)  │
   │   stage → merge → close-day    │
   └────────────────────────────────┘
            │
            ▼
   ┌────────────────────────────────┐
   │   SQL Server                   │
   │   - stg_punches (staging)      │
   │   - fact_attendance (cleaned)  │
   │   - dim_user, dim_date         │
   └────────────────────────────────┘
            │
            ▼
   ┌────────────────────────────────┐
   │   Analytics endpoints          │
   │   GET /kpi/absenteeism         │
   │   GET /kpi/late-arrival        │
   │   GET /daily?from=&to=         │
   └────────────────────────────────┘
            │
            ▼
   ┌────────────────────────────────┐
   │   Power BI / web client        │
   └────────────────────────────────┘
```

---

## Tech stack

| Layer | Tech |
|-------|------|
| Runtime | .NET 8 |
| API | ASP.NET Core minimal APIs |
| Background work | `IHostedService` (start) → Hangfire (later) |
| ORM | EF Core 8 (dapper for hot read paths) |
| DB | SQL Server 2022 (Docker locally) |
| Validation | FluentValidation |
| Mapping | Mapster (lighter than AutoMapper) |
| Logging | Serilog → console + file + OpenTelemetry sink |
| Docs | Swashbuckle + ReDoc |
| Tests | xUnit + Testcontainers + Bogus |
| CI | GitHub Actions (build + test + codeQL) |

---

## Folder structure (final state)

```
attendance-analytics-api/
├── src/
│   ├── Attendance.Api/
│   │   ├── Endpoints/
│   │   │   ├── AttendanceEndpoints.cs
│   │   │   ├── AnalyticsEndpoints.cs
│   │   │   └── HealthEndpoints.cs
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── Attendance.Application/
│   │   ├── Features/
│   │   │   ├── Punch/
│   │   │   ├── DailyRollUp/
│   │   │   └── Kpi/
│   │   └── Common/
│   ├── Attendance.Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Punch.cs
│   │   │   ├── DailyAttendance.cs
│   │   │   └── Holiday.cs
│   │   ├── ValueObjects/
│   │   └── Enums/
│   └── Attendance.Infrastructure/
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/
│       │   └── Migrations/
│       ├── Workers/
│       │   └── DailyCloseWorker.cs
│       └── Services/
├── tests/
│   ├── Attendance.UnitTests/
│   └── Attendance.IntegrationTests/
├── tools/
│   └── SeedData/                    # Bogus-based synthetic generator
├── sql/
│   ├── schema/                      # raw DDL alongside EF migrations
│   ├── procedures/
│   │   └── sp_daily_close.sql       # T-SQL fast path for end-of-day
│   └── seed/
├── docs/
│   ├── architecture.md
│   ├── data-model.md
│   ├── api-reference.md
│   ├── benchmarks.md
│   └── img/
├── .github/workflows/
├── docker-compose.yml
├── Dockerfile
├── .env.example
├── README.md
└── Attendance.sln
```

---

## API surface (build all 8 endpoints)

```
GET    /health                                       # liveness
GET    /health/ready                                 # readiness (DB + worker)

POST   /api/v1/attendance/punch                      # single punch event
POST   /api/v1/attendance/punch/batch                # CSV/JSON batch ingest

GET    /api/v1/attendance/daily?from=&to=&userId=    # daily roll-up
GET    /api/v1/attendance/users/{id}                 # per-user history (paged)
POST   /api/v1/attendance/regularize                 # admin override

GET    /api/v1/analytics/kpi/absenteeism             # absenteeism %
GET    /api/v1/analytics/kpi/late-arrival            # late-arrival count
GET    /api/v1/analytics/kpi/attendance-trend        # 30-day rolling
```

---

## KPIs to implement

| KPI | Formula | Endpoint |
|-----|---------|----------|
| Absenteeism rate | `absences / working_days` | `/analytics/kpi/absenteeism` |
| Punctuality | `on_time_count / total_present` | `/analytics/kpi/late-arrival` |
| Average hours present | `AVG(duration) per user per day` | `/analytics/kpi/hours-trend` |
| Attendance % | `present_days / working_days` | `/attendance/daily` |
| Streak | longest consecutive present days | `/users/{id}/streak` |

---

## Data model (synthetic, generic)

```sql
-- dim_user — synthetic users via Bogus
CREATE TABLE dim_user (
  user_id            INT IDENTITY PRIMARY KEY,
  external_ref       NVARCHAR(64) NOT NULL UNIQUE,   -- e.g. card ID
  full_name          NVARCHAR(200) NOT NULL,
  role               NVARCHAR(40) NOT NULL,          -- student | faculty | staff
  joined_on          DATE NOT NULL,
  is_active          BIT NOT NULL DEFAULT 1,
  created_at         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  updated_at         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- fact_punch — raw events
CREATE TABLE fact_punch (
  punch_id           BIGINT IDENTITY PRIMARY KEY,
  user_id            INT NOT NULL REFERENCES dim_user(user_id),
  punch_at           DATETIME2 NOT NULL,
  device_id          NVARCHAR(64) NOT NULL,
  direction          CHAR(3) NOT NULL,               -- 'IN' | 'OUT'
  ingested_at        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX ix_punch_user_time ON fact_punch(user_id, punch_at);

-- fact_daily_attendance — aggregated by sp_daily_close
CREATE TABLE fact_daily_attendance (
  user_id            INT NOT NULL,
  attendance_date    DATE NOT NULL,
  first_in           DATETIME2 NULL,
  last_out           DATETIME2 NULL,
  duration_minutes   INT NULL,
  status             NVARCHAR(20) NOT NULL,          -- present | absent | half-day | late
  PRIMARY KEY (user_id, attendance_date)
);
```

---

## Build plan (2-3 weekends)

### Weekend 1 — skeleton + ingestion
- [ ] Solution + project skeleton (4 projects, Clean Architecture)
- [ ] EF Core entities + migrations
- [ ] `POST /punch` endpoint with validation
- [ ] Bogus-based seeder generating 5k users + 1M punches
- [ ] Smoke test: `dotnet test`
- [ ] Push to private repo for review

### Weekend 2 — analytics + worker
- [ ] `sp_daily_close` stored proc (the hot path)
- [ ] Background worker calling it nightly
- [ ] All 5 analytics endpoints
- [ ] Swagger fully populated
- [ ] At least 5 integration tests using Testcontainers
- [ ] Benchmark doc: p50/p95/p99 on the hot endpoints

### Weekend 3 — polish
- [ ] README per template
- [ ] Architecture diagram (use draw.io or excalidraw — embed PNG in `docs/img/`)
- [ ] Power BI sample report consuming the API → screenshot in README
- [ ] GitHub Actions CI passing
- [ ] CodeQL workflow added
- [ ] **Move repo from private → public**
- [ ] Pin on profile

---

## Stretch features (only if weekend 4 happens)

- Redis cache layer on KPI endpoints (show cache-hit ratio)
- OpenTelemetry → Jaeger trace screenshots in `docs/`
- Webhook subscription for "absenteeism threshold breached"
- Azure Container Apps deployment with Bicep

---

## What to talk about in the interview

When asked "tell me about a recent project," lead with:

1. **Problem** — institutional attendance is messy: late ingestion, device clock drift, manual regularization
2. **Choice you made** — stage-and-merge over direct insert, because re-runs are common in production
3. **Trade-off** — used SP for daily-close (faster) over LINQ (more testable). Why? p99 went from 4.2s to 380ms
4. **What you'd do differently** — would split into ingestion-svc + analytics-svc if scale crossed 50M punches/day

Have these four bullets ready cold. They turn a "side project" into a "production engineering story."
