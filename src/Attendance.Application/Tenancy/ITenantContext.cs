using Attendance.Domain.ValueObjects;

namespace Attendance.Application.Tenancy;

/// <summary>
/// Scoped service exposing the current request's tenant. Resolved by
/// <see cref="ITenantResolver"/> at the start of each request via middleware.
/// Throws <see cref="NoTenantResolvedException"/> if accessed before resolution.
/// </summary>
public interface ITenantContext
{
    TenantId TenantId { get; }
    bool IsResolved { get; }
}

public sealed class TenantContext : ITenantContext
{
    private TenantId? _tenantId;

    public TenantId TenantId =>
        _tenantId ?? throw new NoTenantResolvedException();

    public bool IsResolved => _tenantId is not null;

    internal void SetTenant(TenantId tenantId)
    {
        if (_tenantId is not null)
        {
            throw new InvalidOperationException(
                "Tenant already set for this request scope.");
        }
        _tenantId = tenantId;
    }
}

public sealed class NoTenantResolvedException : InvalidOperationException
{
    public NoTenantResolvedException()
        : base("No tenant resolved for the current request. Did you forget the X-Tenant-Id header?") { }
}
