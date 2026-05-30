using Attendance.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Attendance.UnitTests.Domain;

public class DirectionTests
{
    [Theory]
    [InlineData("IN", Direction.In)]
    [InlineData("in", Direction.In)]
    [InlineData("In", Direction.In)]
    [InlineData(" out ", Direction.Out)]
    public void Parse_handles_case_and_whitespace(string raw, Direction expected)
    {
        DirectionExtensions.Parse(raw).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("inout")]
    [InlineData("entry")]
    public void Parse_rejects_invalid(string raw)
    {
        FluentActions.Invoking(() => DirectionExtensions.Parse(raw))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToWireString_roundtrips()
    {
        Direction.In.ToWireString().Should().Be("IN");
        Direction.Out.ToWireString().Should().Be("OUT");
    }
}
