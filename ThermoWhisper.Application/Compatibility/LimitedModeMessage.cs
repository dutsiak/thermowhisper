using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Application.Compatibility;

public static class LimitedModeMessage
{
    /// <summary>Text of the one startup popup: intro + one line per problem (localized text, then the detail). Empty when result.IsSupported.</summary>
    public static string Build(CompatibilityResult result, ILocalizer localizer)
    {
        if (result.IsSupported)
        {
            return string.Empty;
        }

        var lines = result.Problems.Select(problem => Describe(problem, localizer));

        return string.Join(Environment.NewLine, lines.Prepend(localizer.Get(TextKeys.LimitedModeIntro)));
    }

    private static string Describe(CompatibilityProblem problem, ILocalizer localizer)
    {
        var text = $"- {localizer.Get(problem.TextKey)}";

        return string.IsNullOrWhiteSpace(problem.Detail) ? text : $"{text} ({problem.Detail})";
    }
}
