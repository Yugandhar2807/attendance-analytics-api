using Attendance.Application.Tenancy;
using Attendance.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Attendance.UnitTests.Application;

public class TenantContextTests
{
    [Fact]
    public void Accessing_before_resolution_throws()
    {
        var ctx = new TenantContext();

        ctx.IsResolved.Should().BeFalse();

        FluentActions.Invoking(() => _ = ctx.TenantId)
            .Should().Throw<NoTenantResolvedException>();
    }

    [Fact]
    public void Setting_tenant_makes_it_available()
    {
        var ctx = new TenantContext();
        ((dynamic)ctx).GetType()
            .GetMethod("SetTenant",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(ctx, new object[] { TenantId.Parse("tenant-a") });

        ctx.IsResolved.Should().BeTrue();
        ctx.TenantId.Value.Should().Be("tenant-a");
    }
}
