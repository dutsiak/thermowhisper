using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.Contract;

public class FakesTests
{
    [Fact]
    public async Task FakePowerService_write_sets_both_limits_and_records_it()
    {
        var power = new FakePowerService();

        await power.WriteAsync(new CpuLimit(20), CancellationToken.None);
        var state = await power.ReadAsync(CancellationToken.None);

        Assert.Equal(new PowerState(new CpuLimit(20), new CpuLimit(20)), state);
        Assert.Equal([new CpuLimit(20)], power.Writes);
        Assert.Equal(1, power.ReadCount);
        Assert.Equal(1, power.MaxConcurrentWrites);
    }

    [Fact]
    public async Task FakePowerService_failed_write_changes_nothing()
    {
        var power = new FakePowerService { WriteError = new PowerControlException("Write", 5) };

        await Assert.ThrowsAsync<PowerControlException>(() => power.WriteAsync(new CpuLimit(20), CancellationToken.None));

        Assert.Empty(power.Writes);
        Assert.True(power.State.Ac.IsFull);
    }

    [Fact]
    public void FakeSettingsStore_stores_percent_and_counts_sets()
    {
        var store = new FakeSettingsStore();

        store.SetQuietProfilePercent(40);

        Assert.Equal(40, store.GetQuietProfilePercent());
        Assert.Equal(1, store.SetCount);
    }

    [Fact]
    public void FakeCompatibilityProbe_throws_the_configured_error()
    {
        var probe = new FakeCompatibilityProbe { Error = new InvalidOperationException() };

        Assert.Throws<InvalidOperationException>(probe.GetFacts);
    }
}
