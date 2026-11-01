using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Application.Power;

public static class ErrorMessage
{
    /// <summary>Localized ErrorKey; with ErrorDetail: text, empty line (Environment.NewLine x2), detail. Throws ArgumentException for a success result.</summary>
    public static string Build(PowerActionResult result, ILocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(localizer);

        if (result.IsSuccess)
        {
            throw new ArgumentException("A success result has no error message.", nameof(result));
        }

        var message = localizer.Get(result.ErrorKey!);

        return result.ErrorDetail is null
            ? message
            : $"{message}{Environment.NewLine}{Environment.NewLine}{result.ErrorDetail}";
    }
}
