using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.UseCases;

public class LoadSettingsTests
{
    [Theory]
    [InlineData(null, 10)]
    [InlineData(30, 30)]
    [InlineData(70, 10)]
    public void Returns_the_stored_quiet_profile_or_the_default(int? stored, int expected)
    {
        var settings = new FakeSettingsStore { QuietProfilePercent = stored };

        var result = new LoadSettings(settings).Execute();

        Assert.Equal(new AppSettings(new CpuLimit(expected)), result);
    }
}
