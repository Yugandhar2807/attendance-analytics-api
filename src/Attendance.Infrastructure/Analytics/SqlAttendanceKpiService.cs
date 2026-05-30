using Attendance.Application.Analytics;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Analytics;

public sealed class SqlAttendanceKpiService : IAttendanceKpiService
{
    private readonly ScopedDbContextFactory _factory;

    public SqlAttendanceKpiService(ScopedDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<AbsenteeismKpi> GetAbsenteeismAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await _factory.CreateAsync(ct);

        var activeUsers = await db.Users.CountAsync(u => u.IsActive, ct);

        var working = CountBusinessDays(from, to);

        var absentRecords = await db.DailyAttendance
            .CountAsync(d => d.AttendanceDate >= from
                && d.AttendanceDate <= to
                && d.Status == AttendanceStatus.Absent, ct);

        var expected = activeUsers * working;
        var pct = expected == 0
            ? 0m
            : decimal.Round(100m * absentRecords / expected, 2);

        return new AbsenteeismKpi(from, to, activeUsers, working, absentRecords, pct);
    }

    public async Task<PunctualityKpi> GetPunctualityAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await _factory.CreateAsync(ct);

        var allPresent = await db.DailyAttendance
            .Where(d => d.AttendanceDate >= from
                && d.AttendanceDate <= to
                && d.Status != AttendanceStatus.Absent)
            .CountAsync(ct);

        var late = await db.DailyAttendance
            .CountAsync(d => d.AttendanceDate >= from
                && d.AttendanceDate <= to
                && d.Status == AttendanceStatus.Late, ct);

        var onTime = allPresent - late;
        var pct = allPresent == 0
            ? 0m
            : decimal.Round(100m * onTime / allPresent, 2);

        return new PunctualityKpi(from, to, allPresent, onTime, late, pct);
    }

    private static int CountBusinessDays(DateOnly from, DateOnly to)
    {
        int count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
            {
                count++;
            }
        }
        return count;
    }
}
