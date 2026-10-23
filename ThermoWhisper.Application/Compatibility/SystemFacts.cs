namespace ThermoWhisper.Application.Compatibility;

/// <param name="MissingPowrprofExports">Empty when the DLL did not load (not checked).</param>
public sealed record SystemFacts(
    Version WindowsVersion,
    bool PowrprofLoaded,
    IReadOnlyList<string> MissingPowrprofExports,
    bool PowercfgExists)
{
    public bool PowrprofUsable => PowrprofLoaded && MissingPowrprofExports.Count == 0;
}
