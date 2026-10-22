namespace ThermoWhisper.Domain.ValueObjects;

public sealed record PowerState
{
    public PowerState(CpuLimit ac, CpuLimit dc)
    {
        Ac = ac ?? throw new ArgumentNullException(nameof(ac));
        Dc = dc ?? throw new ArgumentNullException(nameof(dc));
    }

    public CpuLimit Ac { get; }

    public CpuLimit Dc { get; }

    public bool IsSlowed => !Ac.IsFull || !Dc.IsFull;

    // AC and DC differ only after an external change; the lower value is the one the user sees.
    public CpuLimit Shown => Ac.Percent <= Dc.Percent ? Ac : Dc;
}
