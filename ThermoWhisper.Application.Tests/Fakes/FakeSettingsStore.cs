using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Application.Tests.Fakes;

internal sealed class FakeSettingsStore : ISettingsStore
{
    public int? QuietProfilePercent { get; set; }

    public int SetCount { get; private set; }

    public int? GetQuietProfilePercent() => QuietProfilePercent;

    public void SetQuietProfilePercent(int percent)
    {
        QuietProfilePercent = percent;
        SetCount++;
    }
}
