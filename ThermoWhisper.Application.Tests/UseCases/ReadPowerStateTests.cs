using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.UseCases;

public class ReadPowerStateTests
{
    private readonly FakePowerService _power = new();
    private readonly PowerStatus _status = new();

    private ReadPowerState Create() => new(_power, _status);

    [Fact]
    public async Task Success_returns_state_and_updates_status()
    {
        var expected = new PowerState(new CpuLimit(80), new CpuLimit(60));
        _power.State = expected;

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.State);
        Assert.Equal(expected, _status.Current);
    }

    [Fact]
    public async Task PowerControlException_becomes_failure_and_status_is_unchanged()
    {
        _power.ReadError = new PowerControlException("Read", 5);

        var result = await Create().ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(TextKeys.PowerReadFailed, result.ErrorKey);
        Assert.Equal(_power.ReadError.Message, result.ErrorDetail);
        Assert.Null(_status.Current);
    }

    [Fact]
    public async Task Other_exceptions_propagate()
    {
        _power.OtherReadError = new InvalidOperationException();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create().ExecuteAsync());
    }
}
