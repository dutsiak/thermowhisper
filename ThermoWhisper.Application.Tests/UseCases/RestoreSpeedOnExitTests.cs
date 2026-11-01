using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.UseCases;

public class RestoreSpeedOnExitTests
{
    private readonly FakePowerService _power = new();
    private readonly PowerStatus _status = new();
    private readonly SystemSupport _support = new();

    public RestoreSpeedOnExitTests()
    {
        _support.Set(new CompatibilityResult([]));
    }

    private RestoreSpeedOnExit Create()
    {
        var read = new ReadPowerState(_power, _status);
        return new RestoreSpeedOnExit(_support, read, new ApplyCpuLimit(_power, _support, read));
    }

    private static PowerState State(int ac, int dc) => new(new CpuLimit(ac), new CpuLimit(dc));

    [Theory]
    [InlineData(80, 80)]
    [InlineData(100, 70)]
    public async Task Slowed_system_writes_full_speed_once(int ac, int dc)
    {
        _power.State = State(ac, dc);

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal([CpuLimit.Full], _power.Writes);
    }

    [Fact]
    public async Task Full_speed_system_does_nothing()
    {
        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Limited_mode_skips_the_read_and_the_write()
    {
        _power.State = State(80, 80);
        _support.Set(new CompatibilityResult([new CompatibilityProblem(TextKeys.WindowsTooOld, "9.0")]));

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _power.ReadCount);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Write_failure_is_returned_and_not_thrown()
    {
        _power.State = State(80, 80);
        _power.WriteError = new PowerControlException("Write", 5);

        var result = await Create().ExecuteAsync();

        Assert.Equal(TextKeys.PowerWriteFailed, result.ErrorKey);
    }

    [Fact]
    public async Task Read_failure_is_returned_without_a_write()
    {
        _power.ReadError = new PowerControlException("Read", 5);

        var result = await Create().ExecuteAsync();

        Assert.Equal(TextKeys.PowerReadFailed, result.ErrorKey);
        Assert.Empty(_power.Writes);
    }
}
