using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Attendance.IntegrationTests.Endpoints;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_endpoint_does_not_require_tenant()
    {
        var client = _factory.CreateClient();
        // No X-Tenant-Id header — health is exempt
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        body.Should().NotBeNull();
        body!.status.Should().Be("alive");
    }

    [Fact]
    public async Task Calling_protected_endpoint_without_tenant_returns_400()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/analytics/absenteeism?from=2026-04-01&to=2026-04-30");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_tenant_returns_404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "no-such-tenant");
        var response = await client.GetAsync("/api/v1/analytics/absenteeism?from=2026-04-01&to=2026-04-30");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record HealthBody(string status, DateTimeOffset ts);
}
