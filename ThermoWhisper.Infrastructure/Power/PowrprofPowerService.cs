using System.Runtime.InteropServices;

using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Infrastructure.Power;

/// <summary>Reads and writes the max processor state of the active scheme through powrprof.dll.</summary>
internal sealed class PowrprofPowerService : IPowerService
{
    // The token only stops a call that has not started yet: a native call in progress cannot be cancelled.
    public Task<PowerState> ReadAsync(CancellationToken cancellationToken) =>
        Task.Run(ReadState, cancellationToken);

    public Task WriteAsync(CpuLimit limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(limit);

        var value = (uint)limit.Percent;

        return Task.Run(() => WriteValues(value, value), cancellationToken);
    }

    internal static void ThrowIfFailed(string apiName, uint returnCode)
    {
        if (returnCode != 0)
        {
            throw new PowerControlException(apiName, (int)returnCode);
        }
    }

    internal static CpuLimit ToCpuLimit(string apiName, uint value)
    {
        if (value is < CpuLimit.MinPercent or > CpuLimit.MaxPercent)
        {
            throw new PowerControlException(apiName, PowerControlException.InvalidDataCode);
        }

        return new CpuLimit((int)value);
    }

    internal (uint Ac, uint Dc) ReadValues()
    {
        var scheme = GetActiveScheme();

        ThrowIfFailed(
            nameof(PowrprofNative.PowerReadACValueIndex),
            PowrprofNative.PowerReadACValueIndex(
                IntPtr.Zero, scheme, PowerSettingIds.ProcessorSubgroup, PowerSettingIds.MaxProcessorState, out var ac));
        ThrowIfFailed(
            nameof(PowrprofNative.PowerReadDCValueIndex),
            PowrprofNative.PowerReadDCValueIndex(
                IntPtr.Zero, scheme, PowerSettingIds.ProcessorSubgroup, PowerSettingIds.MaxProcessorState, out var dc));

        return (ac, dc);
    }

    /// <summary>
    /// Writes AC, then DC, then re-activates the scheme so Windows applies the change.
    /// Not atomic: if DC fails after AC succeeded, AC keeps the new value; the caller sees the exception.
    /// </summary>
    internal void WriteValues(uint ac, uint dc)
    {
        var scheme = GetActiveScheme();

        ThrowIfFailed(
            nameof(PowrprofNative.PowerWriteACValueIndex),
            PowrprofNative.PowerWriteACValueIndex(
                IntPtr.Zero, scheme, PowerSettingIds.ProcessorSubgroup, PowerSettingIds.MaxProcessorState, ac));
        ThrowIfFailed(
            nameof(PowrprofNative.PowerWriteDCValueIndex),
            PowrprofNative.PowerWriteDCValueIndex(
                IntPtr.Zero, scheme, PowerSettingIds.ProcessorSubgroup, PowerSettingIds.MaxProcessorState, dc));
        ThrowIfFailed(
            nameof(PowrprofNative.PowerSetActiveScheme),
            PowrprofNative.PowerSetActiveScheme(IntPtr.Zero, scheme));
    }

    private PowerState ReadState()
    {
        var (ac, dc) = ReadValues();

        return new PowerState(
            ToCpuLimit(nameof(PowrprofNative.PowerReadACValueIndex), ac),
            ToCpuLimit(nameof(PowrprofNative.PowerReadDCValueIndex), dc));
    }

    /// <summary>Copies the GUID and frees the system buffer at once (the LocalFree result is not actionable).</summary>
    private static Guid GetActiveScheme()
    {
        ThrowIfFailed(
            nameof(PowrprofNative.PowerGetActiveScheme),
            PowrprofNative.PowerGetActiveScheme(IntPtr.Zero, out var schemePointer));

        try
        {
            return Marshal.PtrToStructure<Guid>(schemePointer);
        }
        finally
        {
            PowrprofNative.LocalFree(schemePointer);
        }
    }
}
