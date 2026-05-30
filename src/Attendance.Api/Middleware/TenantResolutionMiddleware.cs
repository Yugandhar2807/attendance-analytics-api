using Attendance.Application.Tenancy;
using Attendance.Domain.ValueObjects;

namespace Attendance.Api.Middleware;

/// <summary>
/// Reads the X-Tenant-Id header from the incoming request, validates it via
/// <see cref="ITenantResolver"/>, and stores the resolved tenant on the scoped
/// <see cref="ITenantContext"/>. Endpoints decorated with
/// <see cref="Filters.RequireTenantAttribute"/> can then assume the context is populated.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private const string HeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _log;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(
        HttpContext ctx,
        ITenantResolver resolver,
        TenantContext tenantContext)
    {
        // Skip for endpoints that don't require a tenant (health checks, swagger, etc.)
        var path = ctx.Request.Path.Value ?? string.Empty;
        if (IsTenantExempt(path))
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var rawHeader)
            || string.IsNullOrWhiteSpace(rawHeader))
        {
            await WriteProblemAsync(ctx, 400,
                "missing-tenant-header",
                $"Required header '{HeaderName}' is missing.");
            return;
        }

        TenantId tenantId;
        try
        {
            tenantId = TenantId.Parse(rawHeader.ToString());
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(ctx, 400, "invalid-tenant-header", ex.Message);
            return;
        }

        var resolution = await resolver.ResolveAsync(tenantId, ctx.RequestAborted);
        if (resolution is null)
        {
            _log.LogWarning("Unknown tenant {Tenant} from {RemoteIp}",
                tenantId, ctx.Connection.RemoteIpAddress);
            await WriteProblemAsync(ctx, 404, "unknown-tenant",
                $"Tenant '{tenantId}' is not registered or is inactive.");
            return;
        }

        tenantContext.SetTenant(tenantId);
        ctx.Response.Headers["X-Resolved-Tenant"] = tenantId.Value;

        await _next(ctx);
    }

    private static bool IsTenantExempt(string path) =>
        path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteProblemAsync(
        HttpContext ctx, int statusCode, string type, string detail)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            type = $"https://api.example.com/problems/{type}",
            title = type.Replace('-', ' '),
            status = statusCode,
            detail,
            traceId = ctx.TraceIdentifier
        });
    }
}
