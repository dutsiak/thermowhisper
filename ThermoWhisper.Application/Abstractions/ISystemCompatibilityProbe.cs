using ThermoWhisper.Application.Compatibility;

namespace ThermoWhisper.Application.Abstractions;

public interface ISystemCompatibilityProbe
{
    /// <summary>Read only, never throws.</summary>
    SystemFacts GetFacts();
}
