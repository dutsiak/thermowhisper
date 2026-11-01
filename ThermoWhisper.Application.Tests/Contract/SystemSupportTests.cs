using ThermoWhisper.Application.Compatibility;

namespace ThermoWhisper.Application.Tests.Contract;

public class SystemSupportTests
{
    [Fact]
    public void Before_Set_is_not_supported_and_has_no_result()
    {
        var support = new SystemSupport();

        Assert.False(support.IsSupported);
        Assert.Null(support.Result);
    }

    [Fact]
    public void Set_without_problems_is_supported()
    {
        var support = new SystemSupport();

        support.Set(new CompatibilityResult([]));

        Assert.True(support.IsSupported);
    }

    [Fact]
    public void Set_with_a_problem_is_not_supported()
    {
        var support = new SystemSupport();

        support.Set(new CompatibilityResult([new CompatibilityProblem(TextKeys.WindowsTooOld, "9.0")]));

        Assert.False(support.IsSupported);
    }

    [Fact]
    public void Set_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SystemSupport().Set(null!));
    }
}
