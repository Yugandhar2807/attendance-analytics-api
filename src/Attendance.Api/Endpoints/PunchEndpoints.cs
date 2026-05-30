using Attendance.Application.Ingestion;
using Attendance.Application.Tenancy;
using Attendance.Domain.ValueObjects;

namespace Attendance.Api.Endpoints;

public static class PunchEndpoints
{
    public static IEndpointRouteBuilder MapPunchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/punches").WithTags("Punches");

        // -- Mode 1 — single REST punch --
        group.MapPost("/", async (
            PunchRequest req,
            IIngestionStrategy ingestion,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var batchId = $"rest-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

            async IAsyncEnumerable<IncomingPunch> One()
            {
                yield return new IncomingPunch(
                    ExternalRef.Parse(req.ExternalRef),
                    req.PunchAt,
                    req.DeviceId,
                    DirectionExtensions.Parse(req.Direction));
                await Task.CompletedTask;
            }

            var result = await ingestion.IngestAsync(One(), batchId, ct);

            return result.Accepted == 1
                ? Results.Created($"/api/v1/punches/{batchId}", new { batchId, result })
                : Results.UnprocessableEntity(result);
        })
        .WithName("CreatePunch")
        .WithSummary("Ingest a single biometric punch event")
        .Produces(StatusCodes.Status201Created)
        .Produces<IngestionResult>(StatusCodes.Status422UnprocessableEntity);

        // -- Mode 2 — batch CSV upload via multipart (UI flow) --
        group.MapPost("/batch", async (
            HttpRequest request,
            IIngestionStrategy ingestion,
            CsvPunchParser parser,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Expected multipart/form-data" });
            }

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "Form field 'file' required" });
            }

            await using var stream = file.OpenReadStream();
            var batchId = $"ui-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

            var result = await ingestion.IngestAsync(parser.ParseAsync(stream, ct), batchId, ct);
            return Results.Ok(new { batchId, result });
        })
        .WithName("UploadPunchBatch")
        .WithSummary("Upload a CSV of punch events (UI-driven). Header: external_ref,punch_at,device_id,direction")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .DisableAntiforgery();

        // -- Mode 3 — webhook called by PowerShell --
        group.MapPost("/webhook", async (
            PunchWebhookEnvelope env,
            IIngestionStrategy ingestion,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var batchId = env.SourceBatchId ?? $"ps-{Guid.NewGuid():N}";

            async IAsyncEnumerable<IncomingPunch> Stream()
            {
                foreach (var p in env.Punches)
                {
                    yield return new IncomingPunch(
                        ExternalRef.Parse(p.ExternalRef),
                        p.PunchAt,
                        p.DeviceId,
                        DirectionExtensions.Parse(p.Direction));
                }
                await Task.CompletedTask;
            }

            var result = await ingestion.IngestAsync(Stream(), batchId, ct);
            return Results.Ok(new { batchId, result });
        })
        .WithName("ReceivePunchWebhook")
        .WithSummary("PowerShell-friendly webhook receiver — JSON envelope with N punches");

        return app;
    }
}

public sealed record PunchRequest(
    string ExternalRef,
    DateTimeOffset PunchAt,
    string DeviceId,
    string Direction);

public sealed record PunchWebhookEnvelope(
    string? SourceBatchId,
    IReadOnlyList<PunchRequest> Punches);
