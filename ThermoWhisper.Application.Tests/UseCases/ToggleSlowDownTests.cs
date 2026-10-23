using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.UseCases;

public class ToggleSlowDownTests
{
    private readonly FakePowerService _power = new();
    private readonly FakeSettingsStore _settings = new();
    private readonly PowerStatus _status = new();
    private readonly SystemSupport _support = new();

    public ToggleSlowDownTests()
    {
        _support.Set(new CompatibilityResult([]));
    }

    private ToggleSlowDown Create()
    {
        var read = new ReadPowerState(_power, _status);
        return new ToggleSlowDown(_support, read, _settings, new ApplyCpuLimit(_power, _support, read));
    }

    private static PowerState State(int ac, int dc) => new(new CpuLimit(ac), new CpuLimit(dc));

    [Theory]
    [InlineData(30, 30)]
    [InlineData(null, 10)]
    [InlineData(55, 10)]
    public async Task Full_speed_system_writes_the_quiet_profile(int? stored, int expected)
    {
        _settings.QuietProfilePercent = stored;

        await Create().ExecuteAsync();

        Assert.Equal([new CpuLimit(expected)], _power.Writes);
    }

    [Theory]
    [InlineData(80, 80)]
    [InlineData(100, 70)]
    public async Task Slowed_system_writes_full_speed(int ac, int dc)
    {
        _power.State = State(ac, dc);
        _settings.QuietProfilePercent = 30;

        await Create().ExecuteAsync();

        Assert.Equal([CpuLimit.Full], _power.Writes);
    }

    [Fact]
    public async Task Decides_from_a_fresh_read_not_from_PowerStatus()
    {
        _status.Update(State(30, 30));
        _settings.QuietProfilePercent = 30;

        await Create().ExecuteAsync();

        Assert.Equal([new CpuLimit(30)], _power.Writes);
    }

    [Fact]
    public async Task Read_failure_is_returned_without_a_write()
    {
        _power.ReadError = new PowerControlException("Read", 5);

        var result = await Create().ExecuteAsync();

        Assert.Equal(TextKeys.PowerReadFailed, result.ErrorKey);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Limited_mode_fails_without_a_write()
    {
        _support.Set(new CompatibilityResult([new CompatibilityProblem(TextKeys.WindowsTooOld, "9.0")]));

        var result = await Create().ExecuteAsync();

        Assert.Equal(TextKeys.LimitedMode, result.ErrorKey);
        Assert.Empty(_power.Writes);
        Assert.Equal(0, _power.ReadCount);
    }

    [Fact]
    public async Task Other_exceptions_propagate()
    {
        _power.OtherReadError = new InvalidOperationException();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create().ExecuteAsync());
    }
}
