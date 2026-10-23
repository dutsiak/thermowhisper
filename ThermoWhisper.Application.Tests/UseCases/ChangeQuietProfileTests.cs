using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.UseCases;

public class ChangeQuietProfileTests
{
    private readonly FakePowerService _power = new();
    private readonly FakeSettingsStore _settings = new();
    private readonly PowerStatus _status = new();
    private readonly SystemSupport _support = new();

    public ChangeQuietProfileTests()
    {
        _support.Set(new CompatibilityResult([]));
    }

    private ChangeQuietProfile Create()
    {
        var read = new ReadPowerState(_power, _status);
        return new ChangeQuietProfile(_settings, _support, read, new ApplyCpuLimit(_power, _support, read));
    }

    private static PowerState State(int ac, int dc) => new(new CpuLimit(ac), new CpuLimit(dc));

    [Fact]
    public async Task Full_speed_system_saves_the_profile_without_a_write()
    {
        _power.State = State(100, 100);

        var result = await Create().ExecuteAsync(new CpuLimit(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(30, _settings.QuietProfilePercent);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Slowed_system_applies_the_new_profile_at_once()
    {
        _power.State = State(40, 40);

        var result = await Create().ExecuteAsync(new CpuLimit(20));

        Assert.True(result.IsSuccess);
        Assert.Equal(20, _settings.QuietProfilePercent);
        Assert.Equal([new CpuLimit(20)], _power.Writes);
        Assert.Equal(State(20, 20), result.State);
        Assert.Equal(State(20, 20), _status.Current);
    }

    [Fact]
    public async Task Value_outside_the_options_throws_and_saves_nothing()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Create().ExecuteAsync(new CpuLimit(70)));

        Assert.Equal(0, _settings.SetCount);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Limited_mode_saves_the_profile_without_a_read_or_write()
    {
        _support.Set(new CompatibilityResult([new CompatibilityProblem(TextKeys.WindowsTooOld, "9.0")]));

        var result = await Create().ExecuteAsync(new CpuLimit(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(30, _settings.QuietProfilePercent);
        Assert.Equal(0, _power.ReadCount);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Read_failure_keeps_the_saved_profile_and_writes_nothing()
    {
        _power.State = State(40, 40);
        _power.ReadError = new PowerControlException("Read", 5);

        var result = await Create().ExecuteAsync(new CpuLimit(30));

        Assert.Equal(TextKeys.PowerReadFailed, result.ErrorKey);
        Assert.Equal(30, _settings.QuietProfilePercent);
        Assert.Empty(_power.Writes);
    }
}
