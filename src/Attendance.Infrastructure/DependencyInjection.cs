using Attendance.Application.AiAssist;
using Attendance.Application.Analytics;
using Attendance.Application.Ingestion;
using Attendance.Application.Persistence;
using Attendance.Application.Tenancy;
using Attendance.Infrastructure.AiAssist;
using Attendance.Infrastructure.Analytics;
using Attendance.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tenancy catalog (loaded from appsettings.json: TenantCatalog section)
        services.Configure<TenantCatalogOptions>(
            configuration.GetSection("TenantCatalog"));
        services.AddSingleton<ITenantResolver, ConfiguredTenantResolver>();

        // Per-request DbContext factory
        services.AddScoped<ScopedDbContextFactory>();

        // Repositories
        services.AddScoped<IPunchRepository, EfPunchRepository>();
        services.AddScoped<IUserLookup, EfUserLookup>();

        // Ingestion + analytics
        services.AddScoped<IIngestionStrategy, CoreIngestionService>();
        services.AddScoped<CsvPunchParser>();
        services.AddScoped<IAttendanceKpiService, SqlAttendanceKpiService>();

        // AI-assist (optional; gated by config)
        services.Configure<ClaudeOptions>(configuration.GetSection("Claude"));
        services.AddHttpClient<ISchemaInferenceService, ClaudeSchemaInferenceService>(
            (sp, client) =>
            {
                var opts = sp.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<ClaudeOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
