using Attendance.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Attendance.UnitTests.Domain;

public class TenantIdTests
{
    [Theory]
    [InlineData("tenant-a")]
    [InlineData("TENANT_B")]
    [InlineData("acme1")]
    public void Parse_returns_normalized_value_for_valid_input(string raw)
    {
        var t = TenantId.Parse(raw);
        t.Value.Should().Be(raw.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_rejects_empty(string raw)
    {
        FluentActions.Invoking(() => TenantId.Parse(raw))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("tenant a")]     // space
    [InlineData("tenant.a")]     // dot
    [InlineData("tenant/a")]     // slash
    public void Parse_rejects_disallowed_characters(string raw)
    {
        FluentActions.Invoking(() => TenantId.Parse(raw))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_rejects_over_64_chars()
    {
        var raw = new string('a', 65);
        FluentActions.Invoking(() => TenantId.Parse(raw))
            .Should().Throw<ArgumentException>();
    }
}
