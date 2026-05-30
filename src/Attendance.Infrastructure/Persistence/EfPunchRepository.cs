using Attendance.Application.Persistence;
using Attendance.Domain.Entities;
using Attendance.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence;

public sealed class EfPunchRepository : IPunchRepository
{
    private readonly ScopedDbContextFactory _factory;

    public EfPunchRepository(ScopedDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<(int Inserted, int Duplicates)> InsertSkipDuplicatesAsync(
        IReadOnlyList<Punch> punches,
        CancellationToken ct)
    {
        if (punches.Count == 0)
        {
            return (0, 0);
        }

        await using var db = await _factory.CreateAsync(ct);

        // Snapshot what's already there for these punches (uniqueness tuple)
        var keys = punches
            .Select(p => new { p.UserId, p.PunchAt, p.DeviceId, p.Direction })
            .ToList();

        // Pull existing rows for the user-ids touched (cheaper than per-row exists check)
        var userIds = keys.Select(k => k.UserId).Distinct().ToList();
        var minTime = keys.Min(k => k.PunchAt);
        var maxTime = keys.Max(k => k.PunchAt);

        var existing = await db.Punches
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId)
                && p.PunchAt >= minTime
                && p.PunchAt <= maxTime)
            .Select(p => new { p.UserId, p.PunchAt, p.DeviceId, p.Direction })
            .ToListAsync(ct);

        var existingSet = new HashSet<(int, DateTimeOffset, string, Direction)>(
            existing.Select(e => (e.UserId, e.PunchAt, e.DeviceId, e.Direction)));

        var toInsert = punches
            .Where(p => !existingSet.Contains(
                (p.UserId, p.PunchAt, p.DeviceId, p.Direction)))
            .ToList();

        if (toInsert.Count > 0)
        {
            db.Punches.AddRange(toInsert);
            await db.SaveChangesAsync(ct);
        }

        return (toInsert.Count, punches.Count - toInsert.Count);
    }

    public async Task<IReadOnlyList<Punch>> GetForDayAsync(
        int userId, DateOnly date, CancellationToken ct)
    {
        await using var db = await _factory.CreateAsync(ct);
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);

        return await db.Punches
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.PunchAt >= start && p.PunchAt < end)
            .OrderBy(p => p.PunchAt)
            .ToListAsync(ct);
    }
}

public sealed class EfUserLookup : IUserLookup
{
    private readonly ScopedDbContextFactory _factory;

    public EfUserLookup(ScopedDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<int?> FindIdAsync(ExternalRef externalRef, CancellationToken ct)
    {
        await using var db = await _factory.CreateAsync(ct);
        return await db.Users
            .Where(u => u.ExternalRef == externalRef && u.IsActive)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
    }
}
