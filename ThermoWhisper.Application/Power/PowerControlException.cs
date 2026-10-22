namespace ThermoWhisper.Application.Power;

public sealed class PowerControlException : Exception
{
    /// <summary>ERROR_TIMEOUT: powercfg did not finish in 10 s.</summary>
    public const int TimeoutCode = 1460;

    /// <summary>ERROR_INVALID_DATA: unparsable output or a value outside 1-100.</summary>
    public const int InvalidDataCode = 13;

    public PowerControlException(string operation, int errorCode, Exception? innerException = null)
        : base($"{operation} failed with code {errorCode}.", innerException)
    {
        Operation = operation;
        ErrorCode = errorCode;
    }

    public string Operation { get; }

    /// <summary>The powrprof return code, Win32 error or powercfg exit code.</summary>
    public int ErrorCode { get; }
}
