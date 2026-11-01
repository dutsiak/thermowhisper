using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.Power;

public class CurrentLimitDisplayTests
{
    [Fact]
    public void No_state_is_unknown()
    {
        Assert.Equal(new CurrentLimitDisplay(TextKeys.SettingsLimitUnknown, null, null), CurrentLimitDisplay.From(null));
    }

    [Theory]
    [InlineData(100, 100, TextKeys.SettingsLimitFull, null, null)]
    [InlineData(30, 30, TextKeys.SettingsLimitPercent, 30, null)]
    [InlineData(70, 30, TextKeys.SettingsLimitSplit, 70, 30)]
    [InlineData(100, 40, TextKeys.SettingsLimitSplit, 100, 40)]
    public void State_maps_to_text_key_and_numbers(int ac, int dc, string key, int? acPercent, int? dcPercent)
    {
        var display = CurrentLimitDisplay.From(new PowerState(new CpuLimit(ac), new CpuLimit(dc)));

        Assert.Equal(new CurrentLimitDisplay(key, acPercent, dcPercent), display);
    }
}
