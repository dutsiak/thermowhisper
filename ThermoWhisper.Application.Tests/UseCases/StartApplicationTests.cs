using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.UseCases;

public class StartApplicationTests
{
    private readonly FakeCompatibilityProbe _probe = new();
    private readonly FakePowerService _power = new();
    private readonly PowerStatus _status = new();
    private readonly SystemSupport _support = new();

    private StartApplication Create() =>
        new(new CheckCompatibility(_probe, _power, _support), new ReadPowerState(_power, _status));

    [Fact]
    public async Task Supported_system_reads_state_into_status_and_never_writes()
    {
        var expected = new PowerState(new CpuLimit(80), new CpuLimit(60));
        _power.State = expected;

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSupported);
        Assert.True(_support.IsSupported);
        Assert.Equal(expected, _status.Current);
        Assert.Equal(2, _power.ReadCount); // one read inside the check, one by ReadPowerState
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Unsupported_system_returns_problems_and_does_not_read_after_the_check()
    {
        _probe.Facts = new SystemFacts(new Version(10, 0, 17000), true, [], true);

        var result = await Create().ExecuteAsync();

        Assert.False(result.IsSupported);
        Assert.NotEmpty(result.Problems);
        Assert.Equal(1, _power.ReadCount); // the read inside the check only
        Assert.Null(_status.Current);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Failed_power_read_after_a_successful_check_does_not_throw_and_leaves_status_empty()
    {
        _power.ReadErrorFromCall = 2;
        _power.ReadError = new PowerControlException("Read", 5);

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSupported);
        Assert.Null(_status.Current);
        Assert.Empty(_power.Writes);
    }
}
