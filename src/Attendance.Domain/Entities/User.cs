using Attendance.Domain.ValueObjects;

namespace Attendance.Domain.Entities;

/// <summary>
/// A user (student / faculty / staff) at a tenant institution. Lives in the
/// per-tenant database — no TenantId column required, the DB itself is the boundary.
/// </summary>
public sealed class User
{
    private User() { }

    public User(ExternalRef externalRef, string fullName, UserRole role, DateOnly joinedOn)
    {
        ExternalRef = externalRef;
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Role = role;
        JoinedOn = joinedOn;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public int Id { get; private set; }
    public ExternalRef ExternalRef { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateOnly JoinedOn { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum UserRole
{
    Student = 1,
    Faculty = 2,
    Staff = 3
}
