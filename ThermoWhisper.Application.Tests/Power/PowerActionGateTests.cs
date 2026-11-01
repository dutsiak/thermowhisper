using ThermoWhisper.Application.Power;

namespace ThermoWhisper.Application.Tests.Power;

public class PowerActionGateTests
{
    private readonly PowerActionGate _gate = new();

    [Fact]
    public async Task Second_action_while_busy_returns_null_and_does_not_start()
    {
        var release = new TaskCompletionSource<PowerActionResult>();
        var secondStarted = false;
        var first = _gate.TryRunAsync(() => release.Task);

        var second = await _gate.TryRunAsync(() =>
        {
            secondStarted = true;
            return Task.FromResult(PowerActionResult.Success());
        });

        Assert.Null(second);
        Assert.False(secondStarted);
        release.SetResult(PowerActionResult.Success());
        Assert.NotNull(await first);
    }

    [Fact]
    public async Task IsBusy_is_true_while_the_action_runs_and_false_after()
    {
        var release = new TaskCompletionSource<PowerActionResult>();
        Assert.False(_gate.IsBusy);

        var run = _gate.TryRunAsync(() => release.Task);

        Assert.True(_gate.IsBusy);
        release.SetResult(PowerActionResult.Success());
        await run;
        Assert.False(_gate.IsBusy);
    }

    [Fact]
    public async Task BusyChanged_is_raised_when_the_action_starts_and_when_it_ends()
    {
        var seen = new List<bool>();
        _gate.BusyChanged += (_, _) => seen.Add(_gate.IsBusy);

        await _gate.TryRunAsync(() => Task.FromResult(PowerActionResult.Success()));

        Assert.Equal([true, false], seen);
    }

    [Fact]
    public async Task Rejected_action_does_not_raise_BusyChanged()
    {
        var release = new TaskCompletionSource<PowerActionResult>();
        var first = _gate.TryRunAsync(() => release.Task);
        var raised = 0;
        _gate.BusyChanged += (_, _) => raised++;

        await _gate.TryRunAsync(() => Task.FromResult(PowerActionResult.Success()));

        Assert.Equal(0, raised);
        release.SetResult(PowerActionResult.Success());
        await first;
    }

    [Fact]
    public async Task Exception_reaches_the_caller_and_IsBusy_is_false_again()
    {
        var failing = _gate.TryRunAsync(() => throw new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => failing);
        Assert.False(_gate.IsBusy);
        Assert.NotNull(await _gate.TryRunAsync(() => Task.FromResult(PowerActionResult.Success())));
    }

    [Fact]
    public async Task Returns_the_result_of_the_action()
    {
        var expected = PowerActionResult.Failure("Some_Key", "detail");

        var actual = await _gate.TryRunAsync(() => Task.FromResult(expected));

        Assert.Same(expected, actual);
    }
}
