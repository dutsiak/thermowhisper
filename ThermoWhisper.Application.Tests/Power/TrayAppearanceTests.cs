using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.Power;

public class TrayAppearanceTests
{
    [Fact]
    public void No_state_and_not_limited_shows_the_app_title_with_the_normal_icon()
    {
        var appearance = TrayAppearance.From(null, isLimitedMode: false);

        Assert.Equal(new TrayAppearance(TrayIconKind.Normal, TextKeys.AppTitle, null), appearance);
    }

    [Theory]
    [InlineData(100, 100, TrayIconKind.Normal, TextKeys.TrayFullSpeed, null)]
    [InlineData(10, 10, TrayIconKind.Slowed, TextKeys.TrayLimit, 10)]
    [InlineData(70, 100, TrayIconKind.Slowed, TextKeys.TrayLimit, 70)]
    [InlineData(100, 40, TrayIconKind.Slowed, TextKeys.TrayLimit, 40)]
    public void State_shows_the_lower_of_ac_and_dc(int ac, int dc, TrayIconKind icon, string tooltipKey, int? percent)
    {
        var state = new PowerState(new CpuLimit(ac), new CpuLimit(dc));

        var appearance = TrayAppearance.From(state, isLimitedMode: false);

        Assert.Equal(new TrayAppearance(icon, tooltipKey, percent), appearance);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    public void Limited_mode_shows_limited_mode_with_the_normal_icon_for_any_state(int ac, int dc)
    {
        var state = new PowerState(new CpuLimit(ac), new CpuLimit(dc));

        var appearance = TrayAppearance.From(state, isLimitedMode: true);

        Assert.Equal(new TrayAppearance(TrayIconKind.Normal, TextKeys.TrayLimitedMode, null), appearance);
    }

    [Fact]
    public void Limited_mode_without_a_state_shows_limited_mode()
    {
        var appearance = TrayAppearance.From(null, isLimitedMode: true);

        Assert.Equal(new TrayAppearance(TrayIconKind.Normal, TextKeys.TrayLimitedMode, null), appearance);
    }
}
