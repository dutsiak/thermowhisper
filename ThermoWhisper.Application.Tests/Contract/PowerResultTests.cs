using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.Contract;

public class PowerResultTests
{
    [Fact]
    public void Success_carries_the_state()
    {
        var state = new PowerState(CpuLimit.Full, CpuLimit.Full);

        var result = PowerActionResult.Success(state);

        Assert.True(result.IsSuccess);
        Assert.Equal(state, result.State);
    }

    [Fact]
    public void Failure_has_key_detail_and_no_state()
    {
        var result = PowerActionResult.Failure(TextKeys.PowerWriteFailed, "detail");

        Assert.False(result.IsSuccess);
        Assert.Null(result.State);
        Assert.Equal(TextKeys.PowerWriteFailed, result.ErrorKey);
        Assert.Equal("detail", result.ErrorDetail);
    }

    [Fact]
    public void Exception_message_contains_operation_and_code()
    {
        var exception = new PowerControlException("WriteAcValue", PowerControlException.TimeoutCode);

        Assert.Contains("WriteAcValue", exception.Message);
        Assert.Contains("1460", exception.Message);
        Assert.Equal(1460, exception.ErrorCode);
    }
}
