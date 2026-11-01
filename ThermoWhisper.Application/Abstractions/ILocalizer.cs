namespace ThermoWhisper.Application.Abstractions;

public interface ILocalizer
{
    /// <summary>Text for a resource key in the current UI culture. Unknown key: returns the key.</summary>
    string Get(string key);

    /// <summary>Like <see cref="Get"/>, then string.Format with the arguments.</summary>
    string Format(string key, params object?[] args);
}
