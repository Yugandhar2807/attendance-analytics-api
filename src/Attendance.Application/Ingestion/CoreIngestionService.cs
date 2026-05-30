using Attendance.Application.Persistence;
using Attendance.Domain.Entities;
using Attendance.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Attendance.Application.Ingestion;

/// <summary>
/// All 3 ingestion modes flow through this. Maps incoming punches → users,
/// drops duplicates inline, and writes to the per-tenant DB via the
/// repository abstraction. Idempotent per (UserId, PunchAt, DeviceId, Direction).
/// </summary>
public sealed class CoreIngestionService : IIngestionStrategy
{
    private readonly IPunchRepository _repo;
    private readonly IUserLookup _userLookup;
    private readonly ILogger<CoreIngestionService> _log;

    public CoreIngestionService(
        IPunchRepository repo,
        IUserLookup userLookup,
        ILogger<CoreIngestionService> log)
    {
        _repo = repo;
        _userLookup = userLookup;
        _log = log;
    }

    public async Task<IngestionResult> IngestAsync(
        IAsyncEnumerable<IncomingPunch> punches,
        string batchId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new ArgumentException("batchId is required", nameof(batchId));
        }

        var accepted = 0;
        var rejected = 0;
        var duplicates = 0;
        var errors = new List<IngestionError>();
        var buffer = new List<Punch>(capacity: 256);
        var rowIndex = -1;

        await foreach (var p in punches.WithCancellation(ct))
        {
            rowIndex++;
            try
            {
                var userId = await _userLookup.FindIdAsync(p.ExternalRef, ct);
                if (userId is null)
                {
                    rejected++;
                    errors.Add(new IngestionError(rowIndex,
                        $"User with external_ref={p.ExternalRef} not found"));
                    continue;
                }

                buffer.Add(new Punch(
                    userId.Value, p.PunchAt, p.DeviceId, p.Direction, batchId));

                if (buffer.Count >= 256)
                {
                    var (ins, dup) = await _repo.InsertSkipDuplicatesAsync(buffer, ct);
                    accepted += ins;
                    duplicates += dup;
                    buffer.Clear();
                }
            }
            catch (Exception ex)
            {
                rejected++;
                errors.Add(new IngestionError(rowIndex, ex.Message));
            }
        }

        if (buffer.Count > 0)
        {
            var (ins, dup) = await _repo.InsertSkipDuplicatesAsync(buffer, ct);
            accepted += ins;
            duplicates += dup;
        }

        _log.LogInformation(
            "Ingestion {BatchId} complete: accepted={Accepted} duplicates={Duplicates} rejected={Rejected}",
            batchId, accepted, duplicates, rejected);

        return new IngestionResult(accepted, rejected, duplicates, errors);
    }
}
