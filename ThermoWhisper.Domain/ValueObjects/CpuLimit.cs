namespace ThermoWhisper.Domain.ValueObjects;

public sealed record CpuLimit
{
    public const int MinPercent = 1;
    public const int MaxPercent = 100;

    public static CpuLimit Full { get; } = new(MaxPercent);

    public CpuLimit(int percent)
    {
        if (percent is < MinPercent or > MaxPercent)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "CPU limit must be 1-100 %.");
        }

        Percent = percent;
    }

    // Get-only: a `with` expression cannot bypass the validation above.
    public int Percent { get; }

    public bool IsFull => Percent == MaxPercent;

    public override string ToString() => $"{Percent} %";
}
