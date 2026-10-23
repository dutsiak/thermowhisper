using System.ComponentModel;
using System.Diagnostics;

using ThermoWhisper.Application.Power;

namespace ThermoWhisper.Infrastructure.Power;

/// <summary>Runs powercfg.exe directly (no shell, no window). A run longer than the timeout is killed with its tree.</summary>
internal sealed class PowercfgRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly string _exePath;
    private readonly TimeSpan _timeout;

    public PowercfgRunner()
        : this(Path.Combine(Environment.SystemDirectory, "powercfg.exe"), DefaultTimeout)
    {
    }

    internal PowercfgRunner(string exePath, TimeSpan timeout)
    {
        _exePath = exePath;
        _timeout = timeout;
    }

    /// <summary>Returns the standard output when <paramref name="readOutput"/> is set, otherwise an empty string.</summary>
    public async Task<string> RunAsync(IEnumerable<string> arguments, bool readOutput, CancellationToken cancellationToken)
    {
        using var process = await Task.Run(() => Start(arguments, readOutput), cancellationToken);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_timeout);

        try
        {
            var output = readOutput ? process.StandardOutput.ReadToEndAsync(timeoutSource.Token) : Task.FromResult(string.Empty);
            await process.WaitForExitAsync(timeoutSource.Token);

            return process.ExitCode == 0 ? await output : throw Failure(process.ExitCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);

            throw Failure(PowerControlException.TimeoutCode);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);

            throw;
        }
    }

    private Process Start(IEnumerable<string> arguments, bool readOutput)
    {
        var startInfo = new ProcessStartInfo(_exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = readOutput,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            return Process.Start(startInfo) ?? throw Failure(PowerControlException.InvalidDataCode);
        }
        catch (Win32Exception error)
        {
            throw new PowerControlException(Path.GetFileName(_exePath), error.NativeErrorCode, error);
        }
    }

    private PowerControlException Failure(int code) => new(Path.GetFileName(_exePath), code);
}
