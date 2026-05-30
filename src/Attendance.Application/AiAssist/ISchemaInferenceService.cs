namespace Attendance.Application.AiAssist;

/// <summary>
/// AI-assisted schema inference. The real-world use case:
///   "A new tenant sends us a CSV with unfamiliar column names like
///    'CardNo','TimeStamp','DEV','InOut' — figure out the mapping."
/// We send a tiny sample (first 5 rows) to an LLM and parse a structured JSON
/// reply mapping their columns to our canonical columns. Saves engineering hours.
///
/// Implementation in Infrastructure layer uses Anthropic Claude API.
/// </summary>
public interface ISchemaInferenceService
{
    Task<SchemaInferenceResult> InferAsync(
        string csvSample,
        CancellationToken ct);
}

public sealed record SchemaInferenceResult(
    SchemaMapping Mapping,
    decimal Confidence,
    string? ReasoningSummary);

public sealed record SchemaMapping(
    string ExternalRefColumn,
    string PunchAtColumn,
    string DeviceIdColumn,
    string DirectionColumn);
