using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;

namespace ThermoWhisper.Application.Tests.Power;

public class ErrorMessageTests
{
    private readonly FakeLocalizer _localizer = new();

    [Fact]
    public void Failure_with_detail_is_text_empty_line_detail()
    {
        var result = PowerActionResult.Failure(TextKeys.PowerWriteFailed, "code 5");

        var message = ErrorMessage.Build(result, _localizer);

        Assert.Equal($"[{TextKeys.PowerWriteFailed}]{Environment.NewLine}{Environment.NewLine}code 5", message);
    }

    [Fact]
    public void Failure_without_detail_is_only_the_text()
    {
        var result = PowerActionResult.Failure(TextKeys.PowerReadFailed);

        Assert.Equal($"[{TextKeys.PowerReadFailed}]", ErrorMessage.Build(result, _localizer));
    }

    [Fact]
    public void Success_throws()
    {
        Assert.Throws<ArgumentException>(() => ErrorMessage.Build(PowerActionResult.Success(), _localizer));
    }
}
