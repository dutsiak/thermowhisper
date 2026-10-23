using ThermoWhisper.Infrastructure.Compatibility;

namespace ThermoWhisper.Infrastructure.Tests.Compatibility;

/// <summary>Read only: these tests never write power settings. CI runs them on windows-latest.</summary>
public class SystemCompatibilityProbeTests
{
    private static readonly Version MinimumWindowsVersion = new(10, 0, 17763);

    // Fixed test input on purpose: it is the spec from the architecture (section 4), not a copy of production data.
    private static readonly string[] SixPowrprofExports =
    [
        "PowerGetActiveScheme",
        "PowerReadACValueIndex",
        "PowerReadDCValueIndex",
        "PowerWriteACValueIndex",
        "PowerWriteDCValueIndex",
        "PowerSetActiveScheme"
    ];

    private static readonly string ExistingPowercfg = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe");

    private static readonly string MissingPowercfg = Path.Combine(
        Path.GetTempPath(), "thermowhisper-no-such-folder", "powercfg.exe");

    [Fact]
    public void GetFacts_RealSystem_ReportsSupportedWindowsAndPowrprof()
    {
        var facts = new SystemCompatibilityProbe().GetFacts();

        Assert.True(facts.WindowsVersion >= MinimumWindowsVersion);
        Assert.True(facts.PowrprofLoaded);
        Assert.Empty(facts.MissingPowrprofExports);
        Assert.True(facts.PowrprofUsable);
        Assert.True(facts.PowercfgExists);
    }

    [Fact]
    public void GetFacts_MissingLibrary_ReportsNotLoadedAndNoExportCheck()
    {
        var facts = new SystemCompatibilityProbe("thermowhisper-missing.dll", SixPowrprofExports, ExistingPowercfg).GetFacts();

        Assert.False(facts.PowrprofLoaded);
        Assert.Empty(facts.MissingPowrprofExports);
        Assert.False(facts.PowrprofUsable);
    }

    [Fact]
    public void GetFacts_UnknownExport_ReportsOnlyThatExportAsMissing()
    {
        var exports = SixPowrprofExports.Append("NoSuchExport").ToArray();

        var facts = new SystemCompatibilityProbe("powrprof.dll", exports, ExistingPowercfg).GetFacts();

        Assert.True(facts.PowrprofLoaded);
        Assert.Equal(["NoSuchExport"], facts.MissingPowrprofExports);
        Assert.False(facts.PowrprofUsable);
    }

    [Fact]
    public void GetFacts_MissingPowercfg_ReportsNotExisting()
    {
        var facts = new SystemCompatibilityProbe("powrprof.dll", SixPowrprofExports, MissingPowercfg).GetFacts();

        Assert.False(facts.PowercfgExists);
    }
}
