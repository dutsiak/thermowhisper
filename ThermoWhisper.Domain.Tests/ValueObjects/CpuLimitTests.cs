using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Domain.Tests.ValueObjects;

public class CpuLimitTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public static void Constructor_OutOfRange_Throws(int percent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CpuLimit(percent));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public static void Constructor_BoundaryValues_Accepted(int percent)
    {
        Assert.Equal(percent, new CpuLimit(percent).Percent);
    }

    [Fact]
    public static void Full_IsHundredPercentAndFull()
    {
        Assert.Equal(100, CpuLimit.Full.Percent);
        Assert.True(CpuLimit.Full.IsFull);
    }

    [Fact]
    public static void IsFull_BelowHundred_IsFalse()
    {
        Assert.False(new CpuLimit(99).IsFull);
    }

    [Fact]
    public static void Equality_SameValue_Equal()
    {
        Assert.Equal(new CpuLimit(50), new CpuLimit(50));
    }

    [Fact]
    public static void Equality_DifferentValue_NotEqual()
    {
        Assert.NotEqual(new CpuLimit(50), new CpuLimit(60));
    }
}
