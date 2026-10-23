namespace ThermoWhisper.Application.Compatibility;

/// <summary>Singleton. Not supported until the startup check has run.</summary>
public sealed class SystemSupport
{
    private volatile CompatibilityResult? _result;

    public CompatibilityResult? Result => _result;

    public bool IsSupported => Result?.IsSupported == true;

    public void Set(CompatibilityResult result)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
    }
}
