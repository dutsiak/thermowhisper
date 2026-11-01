#if DEBUG
using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;

namespace ThermoWhisper.DebugTools;

/// <summary>
/// Debug builds only (never compiled into Release or the MSIX): with THERMOWHISPER_FORCE_LIMITED=1 the startup check
/// fails (old Windows, no power control), so limited mode can be tried by hand. Reads and writes nothing.
/// </summary>
public sealed class ForcedLimitedModeProbe : ISystemCompatibilityProbe
{
    public const string EnvironmentVariable = "THERMOWHISPER_FORCE_LIMITED";

    public static bool IsRequested => Environment.GetEnvironmentVariable(EnvironmentVariable) == "1";

    public SystemFacts GetFacts() => new(new Version(10, 0, 14393, 0), false, [], false);
}
#endif
