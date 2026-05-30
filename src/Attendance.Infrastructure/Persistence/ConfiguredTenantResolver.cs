using Attendance.Application.Tenancy;
using Attendance.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Attendance.Infrastructure.Persistence;

/// <summary>
/// Showcase resolver: reads tenants from appsettings.json (TenantCatalog section).
/// Production would read from a tenants catalog DB + Key Vault for secrets.
/// </summary>
public sealed class ConfiguredTenantResolver : ITenantResolver
{
    private readonly TenantCatalogOptions _options;

    public ConfiguredTenantResolver(IOptions<TenantCatalogOptions> options)
    {
        _options = options.Value;
    }

    public Task<TenantConnection?> ResolveAsync(TenantId tenantId, CancellationToken ct)
    {
        var entry = _options.Tenants
            .FirstOrDefault(t =>
                string.Equals(t.Id, tenantId.Value, StringComparison.OrdinalIgnoreCase));

        if (entry is null || !entry.IsActive)
        {
            return Task.FromResult<TenantConnection?>(null);
        }

        return Task.FromResult<TenantConnection?>(
            new TenantConnection(TenantId.Parse(entry.Id), entry.DisplayName, entry.ConnectionString));
    }

    public Task<IReadOnlyList<TenantId>> ListActiveAsync(CancellationToken ct)
    {
        IReadOnlyList<TenantId> ids = _options.Tenants
            .Where(t => t.IsActive)
            .Select(t => TenantId.Parse(t.Id))
            .ToList();

        return Task.FromResult(ids);
    }
}

public sealed class TenantCatalogOptions
{
    public List<TenantEntry> Tenants { get; set; } = new();
}

public sealed class TenantEntry
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
