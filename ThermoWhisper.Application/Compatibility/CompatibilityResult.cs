namespace ThermoWhisper.Application.Compatibility;

public sealed record CompatibilityResult(IReadOnlyList<CompatibilityProblem> Problems)
{
    public bool IsSupported => Problems.Count == 0;
}
