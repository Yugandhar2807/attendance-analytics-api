namespace Attendance.Domain.Entities;

/// <summary>
/// One row per (User, Date). Materialized by the daily-close worker from raw punches.
/// </summary>
public sealed class DailyAttendance
{
    private DailyAttendance() { }

    public DailyAttendance(int userId, DateOnly date, AttendanceStatus status)
    {
        UserId = userId;
        AttendanceDate = date;
        Status = status;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    public int UserId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public DateTimeOffset? FirstIn { get; private set; }
    public DateTimeOffset? LastOut { get; private set; }
    public int? DurationMinutes { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public DateTimeOffset ClosedAt { get; private set; }

    public void RecordTiming(DateTimeOffset? firstIn, DateTimeOffset? lastOut)
    {
        FirstIn = firstIn;
        LastOut = lastOut;
        DurationMinutes = firstIn.HasValue && lastOut.HasValue
            ? (int)(lastOut.Value - firstIn.Value).TotalMinutes
            : null;
    }
}

public enum AttendanceStatus
{
    Absent = 0,
    Present = 1,
    HalfDay = 2,
    Late = 3
}
