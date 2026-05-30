using Attendance.Domain.ValueObjects;

namespace Attendance.Domain.Entities;

/// <summary>
/// A raw biometric punch event. Immutable after creation.
/// The (UserId, PunchAt, DeviceId, Direction) tuple is the natural uniqueness key.
/// </summary>
public sealed class Punch
{
    private Punch() { }

    public Punch(
        int userId,
        DateTimeOffset punchAt,
        string deviceId,
        Direction direction,
        string batchId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device id required.", nameof(deviceId));
        }

        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new ArgumentException("Batch id required.", nameof(batchId));
        }

        UserId = userId;
        PunchAt = punchAt;
        DeviceId = deviceId.Trim();
        Direction = direction;
        BatchId = batchId.Trim();
        IngestedAt = DateTimeOffset.UtcNow;
    }

    public long Id { get; private set; }
    public int UserId { get; private set; }
    public DateTimeOffset PunchAt { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public Direction Direction { get; private set; }
    public string BatchId { get; private set; } = string.Empty;
    public DateTimeOffset IngestedAt { get; private set; }
}
