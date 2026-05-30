namespace Attendance.Domain.ValueObjects;

/// <summary>
/// IN / OUT — typed enum prevents "punch direction was 'in'" string bugs.
/// </summary>
public enum Direction
{
    In = 1,
    Out = 2
}

public static class DirectionExtensions
{
    public static Direction Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("Direction cannot be empty.", nameof(raw));
        }

        return raw.Trim().ToUpperInvariant() switch
        {
            "IN" => Direction.In,
            "OUT" => Direction.Out,
            _ => throw new ArgumentException(
                $"Direction must be 'IN' or 'OUT', got '{raw}'.", nameof(raw))
        };
    }

    public static string ToWireString(this Direction direction) =>
        direction switch
        {
            Direction.In => "IN",
            Direction.Out => "OUT",
            _ => throw new InvalidOperationException($"Unknown direction: {direction}")
        };
}
