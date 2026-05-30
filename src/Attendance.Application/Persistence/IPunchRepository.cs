using Attendance.Domain.Entities;

namespace Attendance.Application.Persistence;

/// <summary>
/// Repository abstraction. Implementation lives in Infrastructure (EF Core).
/// Domain talks to this; no EF types leak past this interface.
/// </summary>
public interface IPunchRepository
{
    /// <summary>
    /// Inserts only those punches that don't already exist (by uniqueness key).
    /// Returns (inserted, duplicates).
    /// </summary>
    Task<(int Inserted, int Duplicates)> InsertSkipDuplicatesAsync(
        IReadOnlyList<Punch> punches,
        CancellationToken ct);

    Task<IReadOnlyList<Punch>> GetForDayAsync(int userId, DateOnly date, CancellationToken ct);
}

public interface IUserLookup
{
    Task<int?> FindIdAsync(Attendance.Domain.ValueObjects.ExternalRef externalRef, CancellationToken ct);
}
