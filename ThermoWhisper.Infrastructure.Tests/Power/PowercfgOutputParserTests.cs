using ThermoWhisper.Application.Power;
using ThermoWhisper.Infrastructure.Power;

namespace ThermoWhisper.Infrastructure.Tests.Power;

public class PowercfgOutputParserTests
{
    private const string Sample = PowercfgSample.Output;

    [Fact]
    public void Parse_RealSample_ReturnsValues()
    {
        var state = PowercfgOutputParser.Parse(Sample);

        Assert.Equal(100, state.Ac.Percent);
        Assert.Equal(100, state.Dc.Percent);
    }

    [Fact]
    public void Parse_UkrainianAndGermanLabels_ReturnsSameValues()
    {
        var ukrainian = Sample
            .Replace("Current AC Power Setting Index", "Поточний індекс параметра живлення від мережі")
            .Replace("Current DC Power Setting Index", "Поточний індекс параметра живлення від батареї");
        var german = Sample
            .Replace("Current AC Power Setting Index", "Aktueller Wechselstromeinstellungsindex")
            .Replace("Current DC Power Setting Index", "Aktueller Gleichstromeinstellungsindex");

        Assert.Equal(PowercfgOutputParser.Parse(Sample), PowercfgOutputParser.Parse(ukrainian));
        Assert.Equal(PowercfgOutputParser.Parse(Sample), PowercfgOutputParser.Parse(german));
    }

    [Fact]
    public void Parse_AcThenDc_ReturnsAc80AndDc50()
    {
        var output = "Maximum: 0x00000064\nAC: 0x00000050\nDC: 0x00000032\n";

        var state = PowercfgOutputParser.Parse(output);

        Assert.Equal(80, state.Ac.Percent);
        Assert.Equal(50, state.Dc.Percent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no values here")]
    [InlineData("only one: 0x00000050")]
    [InlineData("AC: 0x00000000\nDC: 0x00000050")]
    [InlineData("AC: 0x00000050\nDC: 0x00000065")]
    public void Parse_UnusableOutput_ThrowsInvalidData(string output)
    {
        var error = Assert.Throws<PowerControlException>(() => PowercfgOutputParser.Parse(output));

        Assert.Equal(PowerControlException.InvalidDataCode, error.ErrorCode);
    }
}
