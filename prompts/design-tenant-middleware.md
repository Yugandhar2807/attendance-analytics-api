# Prompt — Tenant Resolution Middleware

## Context I gave Claude

> I'm building a multi-tenant ASP.NET Core 8 API using minimal APIs.
> Pattern: DB-per-tenant. Each request includes an `X-Tenant-Id` header.
> A middleware needs to:
>
> 1. Read the header
> 2. Validate it via an injected `ITenantResolver`
> 3. Store the resolved tenant on a scoped `ITenantContext`
> 4. Skip resolution for `/health`, `/health/ready`, `/swagger`, and `/`
> 5. Return RFC 7807 problem details on failures (400 / 404)
>
> Constraints:
> - No exceptions for "unknown tenant" — return 404, not 500
> - The scoped `TenantContext` must throw if accessed before resolution
>   (catches "I forgot to add the middleware" bugs at runtime)
> - All log lines from middleware must include the tenant id when resolved
>
> Write the middleware class. Use the file-scoped namespace `Attendance.Api.Middleware`.

## What Claude produced

(See [`TenantResolutionMiddleware.cs`](../src/Attendance.Api/Middleware/TenantResolutionMiddleware.cs))

## What I changed before committing

1. Renamed the protected method `WriteProblem` → `WriteProblemAsync` to match
   the project convention (every Task-returning method ends with `Async`).
2. Added the `X-Resolved-Tenant` response header — useful for debugging in
   network tools (helpful when you have 26 tenants).
3. Made `IsTenantExempt` `static` (codified by `TreatWarningsAsErrors`).
4. Replaced Claude's `WebUtility.HtmlEncode` on the detail message — not needed
   for JSON responses; was a copy-paste from an MVC scenario.

## What I'd ask differently next time

The first attempt missed the response header. Next time I'd include the
"debugging observability" requirement in the constraint list explicitly.
