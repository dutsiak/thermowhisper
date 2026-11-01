using System.Globalization;
using System.Resources;

using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Localization;

public sealed class ResxLocalizer : ILocalizer
{
    private readonly ResourceManager _resources = new("ThermoWhisper.Localization.AppResources", typeof(ResxLocalizer).Assembly);

    public string Get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public string Format(string key, params object?[] args)
    {
        var text = Get(key);

        try
        {
            return string.Format(CultureInfo.CurrentUICulture, text, args);
        }
        catch (FormatException)
        {
            // Used on error paths: a text with a wrong placeholder is shown as it is instead of throwing.
            return text;
        }
    }
}
