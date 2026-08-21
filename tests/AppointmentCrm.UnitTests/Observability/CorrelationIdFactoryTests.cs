using AppointmentCrm.Api.Observability;

namespace AppointmentCrm.UnitTests.Observability;

public sealed class CorrelationIdFactoryTests
{
    [Fact]
    public void Create_NormalizesAValidGuid()
    {
        var source = Guid.Parse("9413B51F-C715-43C1-98AB-474925F6934C");

        var result = CorrelationIdFactory.Create(source.ToString());

        Assert.Equal("9413b51fc71543c198ab474925f6934c", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("attacker-controlled-value")]
    public void Create_ReplacesInvalidInput(string? candidate)
    {
        var result = CorrelationIdFactory.Create(candidate);

        Assert.True(Guid.TryParseExact(result, "N", out _));
        Assert.NotEqual(candidate, result);
    }
}
