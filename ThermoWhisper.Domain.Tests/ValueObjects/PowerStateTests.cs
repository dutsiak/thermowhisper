using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Domain.Tests.ValueObjects;

public class PowerStateTests
{
    [Fact]
    public static void IsSlowed_BothFull_IsFalse()
    {
        Assert.False(CreateState(100, 100).IsSlowed);
    }

    [Theory]
    [InlineData(80, 100)]
    [InlineData(100, 80)]
    public static void IsSlowed_AcOrDcBelowFull_IsTrue(int ac, int dc)
    {
        Assert.True(CreateState(ac, dc).IsSlowed);
    }

    [Theory]
    [InlineData(80, 60, 60)]
    [InlineData(60, 80, 60)]
    [InlineData(100, 100, 100)]
    public static void Shown_IsTheLowerValue(int ac, int dc, int expected)
    {
        Assert.Equal(new CpuLimit(expected), CreateState(ac, dc).Shown);
    }

    [Fact]
    public static void Constructor_NullAc_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new PowerState(null!, CpuLimit.Full));

        Assert.Equal("ac", ex.ParamName);
    }

    [Fact]
    public static void Constructor_NullDc_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new PowerState(CpuLimit.Full, null!));

        Assert.Equal("dc", ex.ParamName);
    }

    [Fact]
    public static void Equality_SameAcAndDc_Equal()
    {
        Assert.Equal(CreateState(80, 60), CreateState(80, 60));
    }

    private static PowerState CreateState(int ac, int dc) => new(new CpuLimit(ac), new CpuLimit(dc));
}
