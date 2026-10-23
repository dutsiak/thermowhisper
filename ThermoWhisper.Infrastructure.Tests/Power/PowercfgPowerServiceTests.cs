using ThermoWhisper.Domain.ValueObjects;
using ThermoWhisper.Infrastructure.Power;
using ThermoWhisper.Infrastructure.Tests.Support;

namespace ThermoWhisper.Infrastructure.Tests.Power;

public class PowercfgPowerServiceTests
{
    private const int TestLimitPercent = 80;

    [Fact]
    public async Task ReadAsync_RealSystem_EqualsPowrprofRead()
    {
        var viaPowercfg = await new PowercfgPowerService().ReadAsync(CancellationToken.None);
        var viaPowrprof = await new PowrprofPowerService().ReadAsync(CancellationToken.None);

        Assert.Equal(viaPowrprof, viaPowercfg);
    }

    // Opt-in: writes 80 % to AC and DC of the active scheme through powercfg, then restores the original values.
    [PowerWriteFact]
    [Trait("Category", "PowerWrite")]
    public async Task WriteAsync_Writes80PercentThroughPowercfg_ThenRestoresOriginals()
    {
        var powrprof = new PowrprofPowerService();
        var (originalAc, originalDc) = powrprof.ReadValues();

        try
        {
            await new PowercfgPowerService().WriteAsync(new CpuLimit(TestLimitPercent), CancellationToken.None);

            var state = await powrprof.ReadAsync(CancellationToken.None);
            Assert.Equal(new PowerState(new CpuLimit(TestLimitPercent), new CpuLimit(TestLimitPercent)), state);
        }
        finally
        {
            powrprof.WriteValues(originalAc, originalDc);
        }

        Assert.Equal((originalAc, originalDc), powrprof.ReadValues());
    }
}
