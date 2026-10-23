using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.Contract;

public class PowerStatusTests
{
    private static PowerState State(int percent) => new(new CpuLimit(percent), new CpuLimit(percent));

    [Fact]
    public void Update_with_first_state_raises_Changed_once_and_sets_Current()
    {
        var status = new PowerStatus();
        var raised = 0;
        status.Changed += (_, _) => raised++;

        status.Update(State(30));

        Assert.Equal(1, raised);
        Assert.Equal(State(30), status.Current);
    }

    [Fact]
    public void Update_with_same_state_raises_nothing()
    {
        var status = new PowerStatus();
        status.Update(State(30));
        var raised = 0;
        status.Changed += (_, _) => raised++;

        status.Update(State(30));

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Update_with_different_state_raises_again()
    {
        var status = new PowerStatus();
        status.Update(State(30));
        var raised = 0;
        status.Changed += (_, _) => raised++;

        status.Update(State(100));

        Assert.Equal(1, raised);
        Assert.Equal(State(100), status.Current);
    }
}
