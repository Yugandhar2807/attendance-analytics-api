namespace Attendance.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new
            {
                status = "alive",
                ts = DateTimeOffset.UtcNow
            }))
            .WithName("Liveness")
            .WithSummary("Liveness probe — am I running?")
            .WithTags("Health");

        app.MapGet("/health/ready", () => Results.Ok(new
            {
                status = "ready",
                ts = DateTimeOffset.UtcNow
            }))
            .WithName("Readiness")
            .WithSummary("Readiness probe — can I serve traffic? (extend to ping DB + cache)")
            .WithTags("Health");

        return app;
    }
}
