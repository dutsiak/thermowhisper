using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;

namespace ThermoWhisper.Application.Tests.Fakes;

internal sealed class FakeCompatibilityProbe : ISystemCompatibilityProbe
{
    public SystemFacts Facts { get; set; } = new(new Version(10, 0, 22631), true, [], true);

    /// <summary>Thrown by GetFacts when set.</summary>
    public Exception? Error { get; set; }

    public SystemFacts GetFacts() => Error is not null ? throw Error : Facts;
}
