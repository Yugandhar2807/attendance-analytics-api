using Attendance.Application.AiAssist;
using Attendance.Application.Tenancy;

namespace Attendance.Api.Endpoints;

public static class AiInferenceEndpoints
{
    public static IEndpointRouteBuilder MapAiInferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai").WithTags("AI-Assisted");

        // POST a small CSV sample, get back the canonical-column mapping.
        // Use case: onboarding a new tenant whose CSV header doesn't match ours.
        group.MapPost("/infer-schema", async (
            SchemaInferenceRequest req,
            ISchemaInferenceService inference,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.CsvSample))
            {
                return Results.BadRequest(new { error = "csvSample required" });
            }

            try
            {
                var result = await inference.InferAsync(req.CsvSample, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(
                    title: "AI service unavailable",
                    detail: ex.Message,
                    statusCode: 503);
            }
        })
        .WithName("InferSchemaFromCsvSample")
        .WithSummary("AI-assisted: map an unknown CSV layout onto the canonical attendance schema. Powered by Claude.")
        .Produces<SchemaInferenceResult>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}

public sealed record SchemaInferenceRequest(string CsvSample);
