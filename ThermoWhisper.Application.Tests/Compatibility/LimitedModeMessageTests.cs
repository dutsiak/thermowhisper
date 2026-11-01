using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Tests.Fakes;

namespace ThermoWhisper.Application.Tests.Compatibility;

public class LimitedModeMessageTests
{
    private readonly FakeLocalizer _localizer = new();

    [Fact]
    public void Message_has_the_intro_then_each_problem_text_and_detail_in_order()
    {
        var result = new CompatibilityResult(
        [
            new CompatibilityProblem(TextKeys.WindowsTooOld, "10.0.14393"),
            new CompatibilityProblem(TextKeys.PowerSchemeUnreadable, "no scheme"),
        ]);

        var message = LimitedModeMessage.Build(result, _localizer);

        var intro = message.IndexOf($"[{TextKeys.LimitedModeIntro}]", StringComparison.Ordinal);
        var first = message.IndexOf($"[{TextKeys.WindowsTooOld}] (10.0.14393)", StringComparison.Ordinal);
        var second = message.IndexOf($"[{TextKeys.PowerSchemeUnreadable}] (no scheme)", StringComparison.Ordinal);
        Assert.Equal(0, intro);
        Assert.InRange(first, 1, int.MaxValue);
        Assert.True(second > first, message);
    }

    [Fact]
    public void Problem_without_detail_shows_only_its_text()
    {
        var result = new CompatibilityResult([new CompatibilityProblem(TextKeys.CompatibilityCheckFailed, " ")]);

        var message = LimitedModeMessage.Build(result, _localizer);

        Assert.EndsWith($"- [{TextKeys.CompatibilityCheckFailed}]", message);
    }

    [Fact]
    public void Supported_result_gives_an_empty_message()
    {
        var message = LimitedModeMessage.Build(new CompatibilityResult([]), _localizer);

        Assert.Equal(string.Empty, message);
    }
}
