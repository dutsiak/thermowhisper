using System.Reflection;
using System.Runtime.InteropServices;

using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;

namespace ThermoWhisper.Infrastructure.Compatibility;

/// <summary>Read only: loads a DLL for a moment and checks its exports, never writes. Never throws.</summary>
public sealed class SystemCompatibilityProbe : ISystemCompatibilityProbe
{
    private const string PowrprofName = "powrprof.dll";

    private static readonly string[] PowrprofExports =
    [
        "PowerGetActiveScheme",
        "PowerReadACValueIndex",
        "PowerReadDCValueIndex",
        "PowerWriteACValueIndex",
        "PowerWriteDCValueIndex",
        "PowerSetActiveScheme"
    ];

    private readonly string _powrprofName;
    private readonly IReadOnlyList<string> _requiredExports;
    private readonly string _powercfgPath;

    public SystemCompatibilityProbe()
        : this(PowrprofName, PowrprofExports, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe"))
    {
    }

    internal SystemCompatibilityProbe(string powrprofName, IReadOnlyList<string> requiredExports, string powercfgPath)
    {
        _powrprofName = powrprofName;
        _requiredExports = requiredExports;
        _powercfgPath = powercfgPath;
    }

    public SystemFacts GetFacts()
    {
        var (loaded, missingExports) = ProbePowrprof();
        return new SystemFacts(
            Environment.OSVersion.Version,
            loaded,
            missingExports,
            File.Exists(_powercfgPath));
    }

    private (bool Loaded, IReadOnlyList<string> MissingExports) ProbePowrprof()
    {
        if (!NativeLibrary.TryLoad(_powrprofName, typeof(SystemCompatibilityProbe).Assembly, DllImportSearchPath.System32, out var handle))
        {
            return (false, []);
        }

        try
        {
            return (true, FindMissingExports(handle));
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    private IReadOnlyList<string> FindMissingExports(IntPtr handle) =>
        _requiredExports.Where(name => !NativeLibrary.TryGetExport(handle, name, out _)).ToArray();
}
