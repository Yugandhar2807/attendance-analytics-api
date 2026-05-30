using System.Globalization;
using Attendance.Domain.ValueObjects;

namespace Attendance.Application.Ingestion;

/// <summary>
/// Minimal CSV parser tuned for the UI-upload flow. Expected header:
///     external_ref,punch_at,device_id,direction
/// Format is ISO 8601 for timestamps. No quoted-field support — the data
/// shape here is simple and we'd rather fail loudly on weird CSV than have
/// CsvHelper silently swallow quotes.
/// </summary>
public sealed class CsvPunchParser
{
    public async IAsyncEnumerable<IncomingPunch> ParseAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);

        var header = await reader.ReadLineAsync(ct);
        if (header is null)
        {
            yield break;
        }

        var cols = header.Split(',').Select(c => c.Trim().ToLowerInvariant()).ToArray();
        var iExt = Array.IndexOf(cols, "external_ref");
        var iAt = Array.IndexOf(cols, "punch_at");
        var iDev = Array.IndexOf(cols, "device_id");
        var iDir = Array.IndexOf(cols, "direction");

        if (iExt < 0 || iAt < 0 || iDev < 0 || iDir < 0)
        {
            throw new FormatException(
                "CSV header must include external_ref, punch_at, device_id, direction");
        }

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length <= Math.Max(iExt, Math.Max(iAt, Math.Max(iDev, iDir))))
            {
                continue; // skip malformed
            }

            var punchAt = DateTimeOffset.Parse(
                parts[iAt].Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            yield return new IncomingPunch(
                ExternalRef.Parse(parts[iExt]),
                punchAt,
                parts[iDev].Trim(),
                DirectionExtensions.Parse(parts[iDir]));
        }
    }
}
