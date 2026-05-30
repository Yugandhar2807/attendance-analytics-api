using Attendance.Domain.ValueObjects;

namespace Attendance.Application.Ingestion;

/// <summary>
/// The 3 ingestion modes (REST / Batch CSV / Webhook from PowerShell) all reduce
/// to the same internal shape — a sequence of <see cref="IncomingPunch"/>.
/// The strategies live here, the wiring lives at the API endpoint layer.
/// </summary>
public interface IIngestionStrategy
{
    Task<IngestionResult> IngestAsync(
        IAsyncEnumerable<IncomingPunch> punches,
        string batchId,
        CancellationToken ct);
}

public sealed record IncomingPunch(
    ExternalRef ExternalRef,
    DateTimeOffset PunchAt,
    string DeviceId,
    Direction Direction);

public sealed record IngestionResult(
    int Accepted,
    int Rejected,
    int Duplicates,
    IReadOnlyList<IngestionError> Errors);

public sealed record IngestionError(int RowIndex, string Reason);
