namespace Attendance.Application.Analytics;

public interface IAttendanceKpiService
{
    Task<AbsenteeismKpi> GetAbsenteeismAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<PunctualityKpi> GetPunctualityAsync(DateOnly from, DateOnly to, CancellationToken ct);
}

public sealed record AbsenteeismKpi(
    DateOnly FromDate,
    DateOnly ToDate,
    int ActiveUsers,
    int WorkingDays,
    int AbsentRecords,
    decimal AbsenteeismPercent);

public sealed record PunctualityKpi(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalPresentRecords,
    int OnTimeRecords,
    int LateRecords,
    decimal OnTimePercent);
