namespace Attendance.Domain.ValueObjects;

/// <summary>
/// Strongly-typed tenant identifier. Prevents the classic bug of passing a userId
/// where a tenantId was expected (string vs string is a compile-time error here).
/// </summary>
public readonly record struct TenantId(string Value)
{
    public static TenantId Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("Tenant id cannot be null or whitespace.", nameof(raw));
        }

        var normalized = raw.Trim().ToLowerInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException("Tenant id must be 64 chars or fewer.", nameof(raw));
        }

        foreach (var c in normalized)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
            {
                throw new ArgumentException(
                    "Tenant id may contain only letters, digits, hyphens, or underscores.",
                    nameof(raw));
            }
        }

        return new TenantId(normalized);
    }

    public override string ToString() => Value;
}
