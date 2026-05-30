using Attendance.Domain.ValueObjects;

namespace Attendance.Application.Tenancy;

/// <summary>
/// Resolves the connection string for a tenant. In production this would look up
/// a tenants catalog table + fetch the connection string from Azure Key Vault.
/// In the showcase the catalog is a JSON file (see appsettings.json).
/// </summary>
public interface ITenantResolver
{
    Task<TenantConnection?> ResolveAsync(TenantId tenantId, CancellationToken ct);

    /// <summary>List all known active tenants. Used by the daily-close worker.</summary>
    Task<IReadOnlyList<TenantId>> ListActiveAsync(CancellationToken ct);
}

public sealed record TenantConnection(TenantId TenantId, string DisplayName, string ConnectionString);
