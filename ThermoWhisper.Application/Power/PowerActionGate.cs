namespace ThermoWhisper.Application.Power;

/// <summary>Singleton shared by tray and Settings: one power action at a time (ToggleSlowDown's read-decide-write is not atomic).</summary>
public sealed class PowerActionGate
{
    private int _busy;

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    /// <summary>Raised on the caller's thread when IsBusy changes; the UI marshals to its own thread.</summary>
    public event EventHandler? BusyChanged;

    /// <summary>Runs the action unless another one runs; returns null when not started. Resets IsBusy in a finally.</summary>
    public async Task<PowerActionResult?> TryRunAsync(Func<Task<PowerActionResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            return null;
        }

        try
        {
            BusyChanged?.Invoke(this, EventArgs.Empty);
            return await action();
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
            BusyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
