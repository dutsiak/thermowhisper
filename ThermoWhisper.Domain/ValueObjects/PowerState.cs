namespace ThermoWhisper.Domain.ValueObjects;

public sealed record PowerState
{
    public PowerState(CpuLimit Ac, CpuLimit Dc)
    {
        this.Ac = Ac ?? throw new ArgumentNullException(nameof(Ac));
        this.Dc = Dc ?? throw new ArgumentNullException(nameof(Dc));
    }

    public CpuLimit Ac { get; }

    public CpuLimit Dc { get; }

    public bool IsSlowed => !Ac.IsFull || !Dc.IsFull;

    // AC and DC differ only after an external change; the lower value is the one the user sees.
    public CpuLimit Shown => Ac.Percent <= Dc.Percent ? Ac : Dc;
}
