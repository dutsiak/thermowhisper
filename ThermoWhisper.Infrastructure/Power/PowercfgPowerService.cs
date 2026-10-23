using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Infrastructure.Power;

/// <summary>Fallback for systems where powrprof.dll cannot be used: the same operations through powercfg.exe.</summary>
internal sealed class PowercfgPowerService : IPowerService
{
    private const string Scheme = "SCHEME_CURRENT";

    private readonly PowercfgRunner _runner;

    public PowercfgPowerService()
        : this(new PowercfgRunner())
    {
    }

    internal PowercfgPowerService(PowercfgRunner runner)
    {
        _runner = runner;
    }

    public async Task<PowerState> ReadAsync(CancellationToken cancellationToken)
    {
        var output = await _runner.RunAsync(SettingArguments("/q"), readOutput: true, cancellationToken);

        return PowercfgOutputParser.Parse(output);
    }

    public async Task WriteAsync(CpuLimit limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(limit);

        var value = limit.Percent.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await _runner.RunAsync(SettingArguments("/setacvalueindex", value), readOutput: false, cancellationToken);
        await _runner.RunAsync(SettingArguments("/setdcvalueindex", value), readOutput: false, cancellationToken);
        await _runner.RunAsync(["/setactive", Scheme], readOutput: false, cancellationToken);
    }

    private static List<string> SettingArguments(string command, string? value = null)
    {
        List<string> arguments =
        [
            command,
            Scheme,
            PowerSettingIds.ProcessorSubgroup.ToString(),
            PowerSettingIds.MaxProcessorState.ToString(),
        ];

        if (value is not null)
        {
            arguments.Add(value);
        }

        return arguments;
    }
}
