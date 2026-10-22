namespace ThermoWhisper.Infrastructure.Tests.Support;

/// <summary>A test that changes the power settings of the machine: runs only when TW_POWER_WRITE_TESTS=1.</summary>
[AttributeUsage(AttributeTargets.Method)]
[Trait("Category", "PowerWrite")]
public sealed class PowerWriteFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "TW_POWER_WRITE_TESTS";

    public PowerWriteFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentVariable) != "1")
        {
            Skip = $"Changes power settings. Set {EnvironmentVariable}=1 to run.";
        }
    }
}
