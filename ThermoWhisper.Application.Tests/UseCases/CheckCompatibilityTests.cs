using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Tests.Fakes;
using ThermoWhisper.Application.UseCases;

namespace ThermoWhisper.Application.Tests.UseCases;

public class CheckCompatibilityTests
{
    private readonly FakeCompatibilityProbe _probe = new();
    private readonly FakePowerService _power = new();
    private readonly SystemSupport _support = new();

    private CheckCompatibility Create() => new(_probe, _power, _support);

    private static SystemFacts Facts(
        Version? windows = null,
        bool powrprofLoaded = true,
        string[]? missingExports = null,
        bool powercfgExists = true) =>
        new(windows ?? new Version(10, 0, 22631), powrprofLoaded, missingExports ?? [], powercfgExists);

    [Fact]
    public async Task Supported_system_has_no_problems_and_is_stored()
    {
        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSupported);
        Assert.Empty(result.Problems);
        Assert.True(_support.IsSupported);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Windows_older_than_minimum_is_one_problem_with_the_version()
    {
        _probe.Facts = Facts(new Version(10, 0, 17134, 0));

        var result = await Create().ExecuteAsync();

        var problem = Assert.Single(result.Problems);
        Assert.Equal(TextKeys.WindowsTooOld, problem.TextKey);
        Assert.Equal("10.0.17134.0", problem.Detail);
    }

    [Fact]
    public async Task Minimum_windows_build_with_revision_zero_is_supported()
    {
        _probe.Facts = Facts(new Version(10, 0, 17763, 0));

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSupported);
    }

    [Fact]
    public async Task Powrprof_unusable_with_powercfg_present_falls_back_and_is_supported()
    {
        _probe.Facts = Facts(powrprofLoaded: false, powercfgExists: true);

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSupported);
        Assert.Equal(1, _power.ReadCount);
    }

    [Fact]
    public async Task Powrprof_usable_with_powercfg_missing_is_supported()
    {
        _probe.Facts = Facts(powercfgExists: false);

        var result = await Create().ExecuteAsync();

        Assert.True(result.IsSupported);
    }

    [Fact]
    public async Task Both_power_paths_missing_is_one_problem_and_the_read_is_not_called()
    {
        _probe.Facts = Facts(powrprofLoaded: false, powercfgExists: false);

        var result = await Create().ExecuteAsync();

        var problem = Assert.Single(result.Problems);
        Assert.Equal(TextKeys.PowerControlUnavailable, problem.TextKey);
        Assert.Contains("powrprof.dll", problem.Detail);
        Assert.Contains("powercfg.exe", problem.Detail);
        Assert.Equal(0, _power.ReadCount);
    }

    [Fact]
    public async Task Missing_powrprof_export_is_named_in_the_problem_detail()
    {
        _probe.Facts = Facts(missingExports: ["PowerSetActiveScheme"], powercfgExists: false);

        var result = await Create().ExecuteAsync();

        var problem = Assert.Single(result.Problems);
        Assert.Equal(TextKeys.PowerControlUnavailable, problem.TextKey);
        Assert.Contains("PowerSetActiveScheme", problem.Detail);
    }

    [Fact]
    public async Task Unreadable_scheme_is_a_problem_with_the_exception_message()
    {
        var exception = new PowerControlException("Read", 5);
        _power.ReadError = exception;

        var result = await Create().ExecuteAsync();

        var problem = Assert.Single(result.Problems);
        Assert.Equal(TextKeys.PowerSchemeUnreadable, problem.TextKey);
        Assert.Equal(exception.Message, problem.Detail);
    }

    [Fact]
    public async Task Other_exception_from_the_read_is_a_problem_and_nothing_is_thrown()
    {
        _power.OtherReadError = new InvalidOperationException("read boom");

        var result = await Create().ExecuteAsync();

        var problem = Assert.Single(result.Problems);
        Assert.Equal(TextKeys.CompatibilityCheckFailed, problem.TextKey);
        Assert.Contains("System.InvalidOperationException", problem.Detail);
        Assert.Contains("read boom", problem.Detail);
    }

    [Fact]
    public async Task Exception_from_the_probe_is_a_problem_and_nothing_is_thrown()
    {
        _probe.Error = new InvalidOperationException("probe boom");

        var result = await Create().ExecuteAsync();

        var problem = Assert.Single(result.Problems);
        Assert.Equal(TextKeys.CompatibilityCheckFailed, problem.TextKey);
        Assert.Contains("probe boom", problem.Detail);
        Assert.Empty(_power.Writes);
    }

    [Fact]
    public async Task Cancellation_is_rethrown()
    {
        _probe.Error = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() => Create().ExecuteAsync());
    }

    [Fact]
    public async Task Several_failures_are_all_listed_in_order()
    {
        _probe.Facts = Facts(new Version(10, 0, 17134, 0));
        _power.ReadError = new PowerControlException("Read", 5);

        var result = await Create().ExecuteAsync();

        Assert.Equal(
            [TextKeys.WindowsTooOld, TextKeys.PowerSchemeUnreadable],
            result.Problems.Select(p => p.TextKey));
    }

    [Fact]
    public async Task Blocked_result_is_stored_and_nothing_is_written()
    {
        _probe.Facts = Facts(new Version(10, 0, 17134, 0), powrprofLoaded: false, powercfgExists: false);

        var result = await Create().ExecuteAsync();

        Assert.False(result.IsSupported);
        Assert.Equal(result, _support.Result);
        Assert.False(_support.IsSupported);
        Assert.Empty(_power.Writes);
    }
}
