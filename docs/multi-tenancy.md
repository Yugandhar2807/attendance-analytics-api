# Multi-Tenancy — DB-per-Tenant Pattern

This is the architecture this showcase implements. It's not the only multi-tenant
pattern, but it's the one that gives the **strongest data isolation** — and the
one used by SaaS products that serve regulated industries (education, healthcare,
finance).

## The three classical patterns

| Pattern | Isolation | Per-tenant cost | Common in |
|---------|-----------|-----------------|-----------|
| **Shared DB, shared schema** (`tenant_id` column on every row) | Lowest | Lowest | High-volume B2C SaaS |
| **Shared DB, separate schema per tenant** | Medium | Medium | Mid-market SaaS |
| **DB-per-tenant** *(this showcase)* | Highest | Highest | Regulated B2B SaaS |

DB-per-tenant trades operational cost for isolation: backups, migrations, and
maintenance run per-database, but a bug that bypasses `WHERE tenant_id =` simply
can't leak across tenants — *the database itself is the boundary*.

## The implementation in this repo

### Three things at request time

```
┌──────────────────────────────────────────────────────────────────────┐
│  1. Request arrives with header X-Tenant-Id: tenant-a                │
│                                                                      │
│  2. TenantResolutionMiddleware                                       │
│        a. parse header → TenantId value object (validates format)    │
│        b. ITenantResolver.ResolveAsync(tenantId) → TenantConnection  │
│        c. store on scoped TenantContext                              │
│        d. set X-Resolved-Tenant response header                      │
│                                                                      │
│  3. Endpoint asks DI for AppDbContext via ScopedDbContextFactory     │
│        a. read TenantContext.TenantId                                │
│        b. resolve the connection string                              │
│        c. build a fresh DbContextOptions with retry policy           │
│        d. return new AppDbContext bound to THAT tenant's DB          │
└──────────────────────────────────────────────────────────────────────┘
```

### Files

| File | Role |
|------|------|
| [`Domain/ValueObjects/TenantId.cs`](../src/Attendance.Domain/ValueObjects/TenantId.cs) | Strongly-typed, format-validated tenant id |
| [`Application/Tenancy/ITenantContext.cs`](../src/Attendance.Application/Tenancy/ITenantContext.cs) | Scoped per-request tenant accessor |
| [`Application/Tenancy/ITenantResolver.cs`](../src/Attendance.Application/Tenancy/ITenantResolver.cs) | tenant id → connection string |
| [`Infrastructure/Persistence/ConfiguredTenantResolver.cs`](../src/Attendance.Infrastructure/Persistence/ConfiguredTenantResolver.cs) | reads from appsettings (production = catalog DB + Key Vault) |
| [`Infrastructure/Persistence/ScopedDbContextFactory.cs`](../src/Attendance.Infrastructure/Persistence/ScopedDbContextFactory.cs) | builds DbContext at request time |
| [`Api/Middleware/TenantResolutionMiddleware.cs`](../src/Attendance.Api/Middleware/TenantResolutionMiddleware.cs) | the request-time entry point |

### What this gives you

- **Cross-tenant data leakage is physically impossible** — DbContext is bound to one DB
- **Per-tenant performance isolation** — a slow query in one tenant doesn't affect others
- **Per-tenant backup / restore** — operationally trivial
- **Per-tenant schema migrations** — useful for staggered rollouts
- **EnableRetryOnFailure** wrapped around every DbContext for transient SQL Server faults

### What it costs

- **Per-tenant DbContext** = no connection pool reuse across tenants
- **No per-tenant pool** is intentional — pooling per tenant defeats the whole point
- **More databases to operate** — ours is 26+ in production; tooling matters
- **Cross-tenant queries are hard** — would require connecting to multiple DBs (acceptable trade)

## Production hardening (not in showcase, but called out)

A real production version of this would add:

1. **Catalog DB** instead of appsettings JSON — `tenants` table with `connection_string_secret_ref`, `is_active`, etc.
2. **Azure Key Vault** for connection strings — the catalog stores a reference, not the secret
3. **Per-tenant DbContextOptions cache** keyed by connection string hash — saves the builder allocation
4. **Caching for ITenantResolver** — current implementation hits the catalog every request; in prod use an `IMemoryCache` with a short TTL + invalidation hook
5. **Tenant-scoped logging context** — every Serilog log line should carry `Enrich.WithProperty("TenantId", ...)` after middleware runs
6. **Authz check** at middleware — verify the calling user / API key is authorized for the claimed tenant
