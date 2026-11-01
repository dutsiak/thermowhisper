using Microsoft.Extensions.Time.Testing;

using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.Power;

public class StatusRefreshLoopTests : IAsyncLifetime
{
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(100);

    private readonly FakePowerService _power = new();
    private readonly PowerStatus _status = new();
    private readonly SystemSupport _support = new();
    private readonly PowerActionGate _gate = new();
    private readonly FakeTimeProvider _time = new();
    private readonly CancellationTokenSource _cts = new();
    private Task _loop = Task.CompletedTask;

    public StatusRefreshLoopTests()
    {
        _support.Set(new CompatibilityResult([]));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _cts.CancelAsync();
        await _loop;
        _cts.Dispose();
    }

    private void StartLoop()
    {
        var loop = new StatusRefreshLoop(new ReadPowerState(_power, _status), _support, _gate, _time);
        _loop = Task.Run(() => loop.RunAsync(_cts.Token));
    }

    private async Task AdvanceAsync(TimeSpan time)
    {
        // Give the loop time to create its timer and to react to the tick.
        await Task.Delay(SettleTime);
        _time.Advance(time);
        await Task.Delay(SettleTime);
    }

    [Fact]
    public async Task Reads_once_per_interval()
    {
        StartLoop();

        await AdvanceAsync(StatusRefreshLoop.Interval - TimeSpan.FromSeconds(1));
        Assert.Equal(0, _power.ReadCount);

        await AdvanceAsync(TimeSpan.FromSeconds(1));
        Assert.Equal(1, _power.ReadCount);

        await AdvanceAsync(StatusRefreshLoop.Interval);
        Assert.Equal(2, _power.ReadCount);
    }

    [Fact]
    public async Task Cancelled_loop_completes_without_exception_and_does_not_read()
    {
        StartLoop();
        await Task.Delay(SettleTime);

        await _cts.CancelAsync();
        await _loop;
        _time.Advance(StatusRefreshLoop.Interval * 2);
        await Task.Delay(SettleTime);

        Assert.True(_loop.IsCompletedSuccessfully);
        Assert.Equal(0, _power.ReadCount);
    }

    [Fact]
    public async Task Failed_read_does_not_stop_the_loop()
    {
        _power.ReadError = new PowerControlException("Read", 5);
        StartLoop();

        await AdvanceAsync(StatusRefreshLoop.Interval);
        _power.ReadError = null;
        await AdvanceAsync(StatusRefreshLoop.Interval);

        Assert.Equal(2, _power.ReadCount);
        Assert.NotNull(_status.Current);
    }

    [Fact]
    public async Task Changed_system_value_updates_status_once()
    {
        var changed = 0;
        _status.Changed += (_, _) => Interlocked.Increment(ref changed);
        _power.State = new PowerState(new CpuLimit(70), new CpuLimit(70));
        StartLoop();

        await AdvanceAsync(StatusRefreshLoop.Interval);

        Assert.Equal(_power.State, _status.Current);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task Tick_is_skipped_while_a_power_action_runs()
    {
        var release = new TaskCompletionSource<PowerActionResult>();
        var action = _gate.TryRunAsync(() => release.Task);
        StartLoop();

        await AdvanceAsync(StatusRefreshLoop.Interval);
        Assert.Equal(0, _power.ReadCount);

        release.SetResult(PowerActionResult.Success());
        await action;
        await AdvanceAsync(StatusRefreshLoop.Interval);
        Assert.Equal(1, _power.ReadCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tick_is_skipped_when_the_system_is_not_supported(bool checkFinished)
    {
        var support = new SystemSupport();

        if (checkFinished)
        {
            support.Set(new CompatibilityResult([new CompatibilityProblem(TextKeys.WindowsTooOld, "test")]));
        }

        var loop = new StatusRefreshLoop(new ReadPowerState(_power, _status), support, _gate, _time);
        _loop = Task.Run(() => loop.RunAsync(_cts.Token));

        await AdvanceAsync(StatusRefreshLoop.Interval);

        Assert.Equal(0, _power.ReadCount);
    }
}
