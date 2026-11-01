using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Application.Tests.Fakes;

/// <summary>Returns "[key]" so a test sees which key was asked for.</summary>
internal sealed class FakeLocalizer : ILocalizer
{
    public string Get(string key) => $"[{key}]";

    public string Format(string key, params object?[] args) => $"[{key}|{string.Join("|", args)}]";
}
