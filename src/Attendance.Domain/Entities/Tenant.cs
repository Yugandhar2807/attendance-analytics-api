using Attendance.Domain.ValueObjects;

namespace Attendance.Domain.Entities;

/// <summary>
/// A tenant — in this showcase, an institution. Each tenant has its own DB.
/// This entity is registered in the **catalog** database (one of the few rows
/// that lives outside per-tenant DBs).
/// </summary>
public sealed class Tenant
{
    private Tenant() { }

    public Tenant(TenantId id, string displayName, string connectionString)
    {
        Id = id;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId Id { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// SQL Server connection string for this tenant's database.
    /// In production this would come from Azure Key Vault, not a column —
    /// the showcase keeps it inline for simplicity.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
