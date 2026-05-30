using Attendance.Api.Endpoints;
using Attendance.Api.Middleware;
using Attendance.Application.Tenancy;
using Attendance.Infrastructure;
using Serilog;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Tenancy + infrastructure
builder.Services.AddTenancy();
builder.Services.AddInfrastructure(builder.Configuration);

// OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Attendance Analytics API",
        Version = "v1",
        Description =
            "Production-pattern multi-tenant attendance API. DB-per-tenant, " +
            "X-Tenant-Id header resolution, 3 ingestion modes, AI-assisted schema inference.",
        Contact = new OpenApiContact
        {
            Name = "Yugandhar N",
            Url = new Uri("https://github.com/Yugandhar2807")
        }
    });
    opt.AddSecurityDefinition("TenantHeader", new OpenApiSecurityScheme
    {
        Name = "X-Tenant-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Tenant identifier (e.g. 'tenant-a'). Required on all /api/* endpoints."
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "TenantHeader"
                }
            },
            Array.Empty<string>()
        }
    });

    var xml = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) opt.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendance Analytics API v1");
    opt.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapHealthEndpoints();
app.MapPunchEndpoints();
app.MapAnalyticsEndpoints();
app.MapAiInferenceEndpoints();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program;
