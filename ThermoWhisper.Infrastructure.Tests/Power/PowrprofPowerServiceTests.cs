using System.Diagnostics;

using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;
using ThermoWhisper.Infrastructure.Power;
using ThermoWhisper.Infrastructure.Tests.Support;

namespace ThermoWhisper.Infrastructure.Tests.Power;

public class PowrprofPowerServiceTests
{
    private const int Eighty = 80;

    [Fact]
    public async Task WriteAsync_NullLimit_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new PowrprofPowerService().WriteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_ReturnsAcAndDcBetween1And100()
    {
        var state = await new PowrprofPowerService().ReadAsync(CancellationToken.None);

        Assert.InRange(state.Ac.Percent, 1, 100);
        Assert.InRange(state.Dc.Percent, 1, 100);
    }

    [Fact]
    public void ThrowIfFailed_Zero_DoesNothing()
    {
        PowrprofPowerService.ThrowIfFailed("PowerGetActiveScheme", 0);
    }

    [Fact]
    public void ThrowIfFailed_NonZero_ThrowsWithApiNameAndCode()
    {
        var error = Assert.Throws<PowerControlException>(() => PowrprofPowerService.ThrowIfFailed("PowerGetActiveScheme", 5));

        Assert.Equal(5, error.ErrorCode);
        Assert.Equal("PowerGetActiveScheme", error.Operation);
    }

    [Theory]
    [InlineData(1u, 1)]
    [InlineData(80u, 80)]
    [InlineData(100u, 100)]
    public void ToCpuLimit_ValueInRange_ReturnsLimit(uint value, int percent)
    {
        Assert.Equal(percent, PowrprofPowerService.ToCpuLimit("PowerReadACValueIndex", value).Percent);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(101u)]
    [InlineData(uint.MaxValue)]
    public void ToCpuLimit_ValueOutOfRange_ThrowsInvalidData(uint value)
    {
        var error = Assert.Throws<PowerControlException>(() => PowrprofPowerService.ToCpuLimit("PowerReadACValueIndex", value));

        Assert.Equal(PowerControlException.InvalidDataCode, error.ErrorCode);
        Assert.Equal("PowerReadACValueIndex", error.Operation);
    }

    // Opt-in: writes 80 % to AC and DC of the active scheme, then restores the original values.
    [PowerWriteFact]
    public async Task WriteAsync_Writes80PercentToAcAndDc_ThenRestoresOriginals()
    {
        var service = new PowrprofPowerService();
        var (originalAc, originalDc) = service.ReadValues();

        try
        {
            await service.WriteAsync(new CpuLimit(Eighty), CancellationToken.None);

            var state = await service.ReadAsync(CancellationToken.None);
            Assert.Equal(new PowerState(new CpuLimit(Eighty), new CpuLimit(Eighty)), state);
            Assert.Equal(2, CountOccurrences(await ReadPowercfgAsync(), Eighty.ToString("x8")));
        }
        finally
        {
            service.WriteValues(originalAc, originalDc);
        }

        Assert.Equal((originalAc, originalDc), service.ReadValues());
    }

    private static async Task<string> ReadPowercfgAsync()
    {
        var startInfo = new ProcessStartInfo("powercfg.exe")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("/q");
        startInfo.ArgumentList.Add("SCHEME_CURRENT");
        startInfo.ArgumentList.Add(PowerSettingIds.ProcessorSubgroup.ToString());
        startInfo.ArgumentList.Add(PowerSettingIds.MaxProcessorState.ToString());

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("powercfg did not start.");
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    private static int CountOccurrences(string text, string value) =>
        text.Split(value).Length - 1;
}
