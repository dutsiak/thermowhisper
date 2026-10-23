using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.UseCases;

/// <summary>Writes the limit and re-reads the real state. One write at a time; a write error wins over a read error.</summary>
public sealed class ApplyCpuLimit(IPowerService power, SystemSupport support, ReadPowerState readPowerState)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<PowerActionResult> ExecuteAsync(CpuLimit limit, CancellationToken cancellationToken = default)
    {
        if (!support.IsSupported)
        {
            return PowerActionResult.Failure(TextKeys.LimitedMode);
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return await WriteAndReadAsync(limit, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PowerActionResult> WriteAndReadAsync(CpuLimit limit, CancellationToken cancellationToken)
    {
        string? writeError = null;

        try
        {
            await power.WriteAsync(limit, cancellationToken);
        }
        catch (PowerControlException exception)
        {
            writeError = exception.Message;
        }

        var readResult = await readPowerState.ExecuteAsync(cancellationToken);

        return writeError is null ? readResult : PowerActionResult.Failure(TextKeys.PowerWriteFailed, writeError);
    }
}
