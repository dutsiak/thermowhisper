using System.Runtime.InteropServices;

using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Infrastructure.Power;

/// <summary>Reads and writes the max processor state of the active scheme through powrprof.dll.</summary>
internal sealed class PowrprofPowerService : IPowerService
{
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

    internal (uint Ac, uint Dc) ReadValues() =>
        WithActiveScheme(scheme =>
        {
            ThrowIfFailed(
                nameof(PowrprofNative.PowerReadACValueIndex),
                PowrprofNative.PowerReadACValueIndex(
                    IntPtr.Zero, scheme, PowerSettingIds.ProcessorSubgroup, PowerSettingIds.MaxProcessorState, out var ac));
            ThrowIfFailed(
                nameof(PowrprofNative.PowerReadDCValueIndex),
                PowrprofNative.PowerReadDCValueIndex(
                    IntPtr.Zero, scheme, PowerSettingIds.ProcessorSubgroup, PowerSettingIds.MaxProcessorState, out var dc));

            return (ac, dc);
        });

    /// <summary>Writes AC, then DC, then re-activates the scheme so Windows applies the change.</summary>
    internal void WriteValues(uint ac, uint dc) =>
        WithActiveScheme(scheme =>
        {
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

            return true;
        });

    private PowerState ReadState()
    {
        var (ac, dc) = ReadValues();

        return new PowerState(
            ToCpuLimit(nameof(PowrprofNative.PowerReadACValueIndex), ac),
            ToCpuLimit(nameof(PowrprofNative.PowerReadDCValueIndex), dc));
    }

    private static T WithActiveScheme<T>(Func<Guid, T> action)
    {
        ThrowIfFailed(
            nameof(PowrprofNative.PowerGetActiveScheme),
            PowrprofNative.PowerGetActiveScheme(IntPtr.Zero, out var schemePointer));

        try
        {
            return action(Marshal.PtrToStructure<Guid>(schemePointer));
        }
        finally
        {
            PowrprofNative.LocalFree(schemePointer);
        }
    }
}
