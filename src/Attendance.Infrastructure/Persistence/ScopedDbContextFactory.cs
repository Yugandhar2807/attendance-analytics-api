using Attendance.Application.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence;

/// <summary>
/// Builds an AppDbContext bound to the current tenant's connection string,
/// resolved fresh per request. THIS is the core of the DB-per-tenant pattern:
/// one DbContext per request, connection string supplied at request time, not at
/// startup. No per-tenant DbContextPool — that would defeat the purpose.
/// </summary>
public sealed class ScopedDbContextFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantResolver _tenantResolver;

    public ScopedDbContextFactory(ITenantContext tenantContext, ITenantResolver tenantResolver)
    {
        _tenantContext = tenantContext;
        _tenantResolver = tenantResolver;
    }

    public async Task<AppDbContext> CreateAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var connection = await _tenantResolver.ResolveAsync(tenantId, ct)
            ?? throw new InvalidOperationException(
                $"Tenant '{tenantId}' is not registered.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection.ConnectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                sql.CommandTimeout(30);
            })
            .Options;

        return new AppDbContext(options);
    }
}
