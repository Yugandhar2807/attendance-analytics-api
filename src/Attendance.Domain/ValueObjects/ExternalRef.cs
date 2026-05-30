namespace Attendance.Domain.ValueObjects;

/// <summary>
/// External reference for a user — typically a card / biometric ID assigned by
/// the source device. Format constrained for safety: 1..64 chars, alphanumeric +
/// hyphen + underscore. We normalize case-insensitively on input.
/// </summary>
public readonly record struct ExternalRef(string Value)
{
    public static ExternalRef Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("External ref cannot be empty.", nameof(raw));
        }

        var normalized = raw.Trim().ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException("External ref must be 64 chars or fewer.", nameof(raw));
        }

        return new ExternalRef(normalized);
    }

    public override string ToString() => Value;
}
