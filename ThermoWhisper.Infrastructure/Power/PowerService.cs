using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Infrastructure.Power;

/// <summary>
/// Power control through powrprof; switches to powercfg for the rest of the run only when powrprof.dll or one of
/// its functions is missing. A powrprof error code is an error, not a reason to switch.
/// </summary>
public sealed class PowerService : IPowerService
{
    private readonly IPowerService _primary;
    private readonly IPowerService _fallback;
    private volatile bool _useFallback;

    public PowerService()
        : this(new PowrprofPowerService(), new PowercfgPowerService())
    {
    }

    internal PowerService(IPowerService primary, IPowerService fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public Task<PowerState> ReadAsync(CancellationToken cancellationToken) =>
        RunAsync(service => service.ReadAsync(cancellationToken));

    public Task WriteAsync(CpuLimit limit, CancellationToken cancellationToken) =>
        RunAsync(async service =>
        {
            await service.WriteAsync(limit, cancellationToken);

            return true;
        });

    private async Task<T> RunAsync<T>(Func<IPowerService, Task<T>> call)
    {
        if (!_useFallback)
        {
            try
            {
                return await call(_primary);
            }
            catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException)
            {
                _useFallback = true;
            }
        }

        return await call(_fallback);
    }
}
