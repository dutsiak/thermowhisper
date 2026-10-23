using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Tests.Fakes;

namespace ThermoWhisper.Application.Tests.Contract;

public class SystemFactsTests
{
    private static readonly SystemFacts Healthy = new FakeCompatibilityProbe().GetFacts();

    [Fact]
    public void PowrprofUsable_when_loaded_and_nothing_missing()
    {
        Assert.True(Healthy.PowrprofUsable);
    }

    [Fact]
    public void PowrprofUsable_is_false_when_not_loaded()
    {
        Assert.False((Healthy with { PowrprofLoaded = false }).PowrprofUsable);
    }

    [Fact]
    public void PowrprofUsable_is_false_when_an_export_is_missing()
    {
        Assert.False((Healthy with { MissingPowrprofExports = ["PowerWriteACValueIndex"] }).PowrprofUsable);
    }
}
