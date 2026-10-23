using System.Runtime.InteropServices;

namespace ThermoWhisper.Infrastructure.Power;

/// <summary>
/// Signatures from the Microsoft docs (powersetting.h / winbase.h). Every power function returns a Win32 error code
/// (0 = success); the root key is always NULL (IntPtr.Zero).
/// </summary>
internal static partial class PowrprofNative
{
    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint PowerReadACValueIndex(
        IntPtr rootPowerKey, in Guid schemeGuid, in Guid subGroupGuid, in Guid powerSettingGuid, out uint acValueIndex);

    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint PowerReadDCValueIndex(
        IntPtr rootPowerKey, in Guid schemeGuid, in Guid subGroupGuid, in Guid powerSettingGuid, out uint dcValueIndex);

    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint PowerWriteACValueIndex(
        IntPtr rootPowerKey, in Guid schemeGuid, in Guid subGroupGuid, in Guid powerSettingGuid, uint acValueIndex);

    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint PowerWriteDCValueIndex(
        IntPtr rootPowerKey, in Guid schemeGuid, in Guid subGroupGuid, in Guid powerSettingGuid, uint dcValueIndex);

    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint PowerSetActiveScheme(IntPtr userRootPowerKey, in Guid schemeGuid);

    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial IntPtr LocalFree(IntPtr memory);
}
