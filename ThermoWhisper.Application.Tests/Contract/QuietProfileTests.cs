using ThermoWhisper.Application.Settings;

namespace ThermoWhisper.Application.Tests.Contract;

public class QuietProfileTests
{
    [Theory]
    [InlineData(null, 10)]
    [InlineData(30, 30)]
    [InlineData(55, 10)]
    [InlineData(0, 10)]
    [InlineData(100, 10)]
    [InlineData(-1, 10)]
    [InlineData(int.MinValue, 10)]
    public void FromStored_returns_option_or_default(int? stored, int expected)
    {
        Assert.Equal(expected, QuietProfile.FromStored(stored).Percent);
    }

    [Fact]
    public void Options_are_ten_to_fifty_in_order()
    {
        Assert.Equal([10, 20, 30, 40, 50], QuietProfile.Options.Select(o => o.Percent));
    }
}
