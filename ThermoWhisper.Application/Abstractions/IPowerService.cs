using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Abstractions;

/// <summary>
/// Max processor state of the ACTIVE power scheme.
/// Never blocks the caller's thread; a failed system call throws only <see cref="Power.PowerControlException"/>.
/// </summary>
public interface IPowerService
{
    Task<PowerState> ReadAsync(CancellationToken cancellationToken);

    /// <summary>Writes the same value to AC and DC, then re-activates the scheme.</summary>
    Task WriteAsync(CpuLimit limit, CancellationToken cancellationToken);
}
