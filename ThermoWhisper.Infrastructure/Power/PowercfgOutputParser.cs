using System.Text.RegularExpressions;

using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Infrastructure.Power;

/// <summary>
/// Parses `powercfg /q SCHEME_CURRENT {subgroup} {setting}`. Labels are localized, so they are never read:
/// the last two 0x hex values of the output are the AC and then the DC index.
/// </summary>
internal static partial class PowercfgOutputParser
{
    private const string OperationName = "powercfg /q";

    public static PowerState Parse(string output)
    {
        var values = HexValue().Matches(output);

        if (values.Count < 2)
        {
            throw new PowerControlException(OperationName, PowerControlException.InvalidDataCode);
        }

        return new PowerState(ToLimit(values[^2].Value), ToLimit(values[^1].Value));
    }

    private static CpuLimit ToLimit(string hex) =>
        PowrprofPowerService.ToCpuLimit(OperationName, Convert.ToUInt32(hex, 16));

    // At most 8 digits, so Convert.ToUInt32 cannot overflow; longer runs of digits are not a setting index.
    [GeneratedRegex("0x[0-9a-fA-F]{1,8}(?![0-9a-fA-F])")]
    private static partial Regex HexValue();
}
