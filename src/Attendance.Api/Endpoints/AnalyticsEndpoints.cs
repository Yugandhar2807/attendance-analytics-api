using Attendance.Application.Analytics;
using Attendance.Application.Tenancy;

namespace Attendance.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/analytics").WithTags("Analytics");

        group.MapGet("/absenteeism", async (
            DateOnly from,
            DateOnly to,
            IAttendanceKpiService kpi,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            if (to < from)
            {
                return Results.BadRequest(new { error = "to must be >= from" });
            }
            var result = await kpi.GetAbsenteeismAsync(from, to, ct);
            return Results.Ok(result);
        })
        .WithName("GetAbsenteeismKpi")
        .WithSummary("Absenteeism % over a date range");

        group.MapGet("/punctuality", async (
            DateOnly from,
            DateOnly to,
            IAttendanceKpiService kpi,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            if (to < from)
            {
                return Results.BadRequest(new { error = "to must be >= from" });
            }
            var result = await kpi.GetPunctualityAsync(from, to, ct);
            return Results.Ok(result);
        })
        .WithName("GetPunctualityKpi")
        .WithSummary("On-time % over a date range");

        return app;
    }
}
