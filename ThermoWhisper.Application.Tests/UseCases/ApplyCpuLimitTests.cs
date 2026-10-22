using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.UseCases;

public class ApplyCpuLimitTests
{
    private readonly FakePowerService _power = new();
    private readonly PowerStatus _status = new();
    private readonly SystemSupport _support = new();

    public ApplyCpuLimitTests()
    {
        _support.Set(new CompatibilityResult([]));
    }

    private ApplyCpuLimit Create() => new(_power, _support, new ReadPowerState(_power, _status));

    [Fact]
    public async Task Writes_once_then_rereads_and_updates_status()
    {
        var expected = new PowerState(new CpuLimit(40), new CpuLimit(40));

        var result = await Create().ExecuteAsync(new CpuLimit(40));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.State);
        Assert.Equal(expected, _status.Current);
        Assert.Equal([new CpuLimit(40)], _power.Writes);
        Assert.Equal(1, _power.ReadCount);
    }

    [Fact]
    public async Task Limited_mode_when_check_found_a_problem_does_nothing()
    {
        _support.Set(new CompatibilityResult([new CompatibilityProblem(TextKeys.WindowsTooOld, "9.0")]));

        await AssertLimitedModeAsync(Create());
    }

    [Fact]
    public async Task Limited_mode_when_support_was_never_set_does_nothing()
    {
        var apply = new ApplyCpuLimit(_power, new SystemSupport(), new ReadPowerState(_power, _status));

        await AssertLimitedModeAsync(apply);
    }

    [Fact]
    public async Task Write_error_wins_and_status_shows_the_real_state()
    {
        _power.WriteError = new PowerControlException("Write", 5);

        var result = await Create().ExecuteAsync(new CpuLimit(40));

        Assert.Equal(TextKeys.PowerWriteFailed, result.ErrorKey);
        Assert.Equal(_power.WriteError.Message, result.ErrorDetail);
        Assert.Equal(1, _power.ReadCount);
        Assert.Equal(new PowerState(CpuLimit.Full, CpuLimit.Full), _status.Current);
    }

    [Fact]
    public async Task Concurrent_calls_write_one_at_a_time_in_call_order()
    {
        var release = new TaskCompletionSource();
        _power.BeforeWrite = () => release.Task;
        var apply = Create();

        var first = apply.ExecuteAsync(new CpuLimit(20));
        var second = apply.ExecuteAsync(new CpuLimit(30));
        await Task.Delay(50);
        release.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, _power.MaxConcurrentWrites);
        Assert.Equal([new CpuLimit(20), new CpuLimit(30)], _power.Writes);
    }

    private async Task AssertLimitedModeAsync(ApplyCpuLimit apply)
    {
        var result = await apply.ExecuteAsync(new CpuLimit(40));

        Assert.Equal(TextKeys.LimitedMode, result.ErrorKey);
        Assert.Empty(_power.Writes);
        Assert.Equal(0, _power.ReadCount);
    }
}
