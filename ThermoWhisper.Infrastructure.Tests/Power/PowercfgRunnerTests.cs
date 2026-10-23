using ThermoWhisper.Application.Power;
using ThermoWhisper.Infrastructure.Power;

namespace ThermoWhisper.Infrastructure.Tests.Power;

public class PowercfgRunnerTests
{
    private static readonly string Cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");

    [Fact]
    public async Task RunAsync_ReadOutput_ReturnsStandardOutput()
    {
        var runner = new PowercfgRunner(Cmd, TimeSpan.FromSeconds(10));

        var output = await runner.RunAsync(["/c", "echo hello"], readOutput: true, CancellationToken.None);

        Assert.Contains("hello", output);
    }

    [Fact]
    public async Task RunAsync_NonZeroExit_ThrowsWithExitCode()
    {
        var runner = new PowercfgRunner(Cmd, TimeSpan.FromSeconds(10));

        var error = await Assert.ThrowsAsync<PowerControlException>(
            () => runner.RunAsync(["/c", "exit 3"], readOutput: false, CancellationToken.None));

        Assert.Equal(3, error.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_LongerThanTimeout_KillsAndThrowsTimeout()
    {
        var runner = new PowercfgRunner(Cmd, TimeSpan.FromSeconds(1));

        var error = await Assert.ThrowsAsync<PowerControlException>(
            () => runner.RunAsync(["/c", "ping -n 30 127.0.0.1 > nul"], readOutput: false, CancellationToken.None));

        Assert.Equal(PowerControlException.TimeoutCode, error.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_MissingExe_ThrowsWin32Code()
    {
        var missing = Path.Combine(Environment.SystemDirectory, "tw-missing-tool.exe");
        var runner = new PowercfgRunner(missing, TimeSpan.FromSeconds(10));

        var error = await Assert.ThrowsAsync<PowerControlException>(
            () => runner.RunAsync([], readOutput: false, CancellationToken.None));

        Assert.Equal(2, error.ErrorCode);
    }
}
