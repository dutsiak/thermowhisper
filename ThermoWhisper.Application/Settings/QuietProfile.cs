using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Settings;

public static class QuietProfile
{
    public static CpuLimit Default { get; } = new(10);

    public static IReadOnlyList<CpuLimit> Options { get; } = [new(10), new(20), new(30), new(40), new(50)];

    public static bool IsOption(CpuLimit limit) => Options.Contains(limit);

    public static CpuLimit FromStored(int? percent) => Options.FirstOrDefault(o => o.Percent == percent) ?? Default;
}
