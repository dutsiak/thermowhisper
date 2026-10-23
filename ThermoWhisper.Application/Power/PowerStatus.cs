using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Power;

/// <summary>Singleton. The last state read, for display only; kept in memory.</summary>
public sealed class PowerStatus
{
    private readonly Lock _lock = new();
    private PowerState? _current;

    /// <summary>Raised on the caller's thread, only when the state differs. The UI marshals to its own thread.</summary>
    public event EventHandler? Changed;

    public PowerState? Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public void Update(PowerState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_lock)
        {
            if (state == _current)
            {
                return;
            }

            _current = state;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
