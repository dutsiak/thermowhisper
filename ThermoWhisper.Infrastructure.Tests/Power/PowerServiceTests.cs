using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;
using ThermoWhisper.Infrastructure.Power;

namespace ThermoWhisper.Infrastructure.Tests.Power;

public class PowerServiceTests
{
    private static readonly PowerState State = new(new CpuLimit(80), new CpuLimit(50));

    [Fact]
    public async Task ReadAsync_PrimaryWorks_FallbackNeverCalled()
    {
        var primary = new StubPowerService();
        var fallback = new StubPowerService();

        var state = await new PowerService(primary, fallback).ReadAsync(CancellationToken.None);

        Assert.Equal(State, state);
        Assert.Equal(0, fallback.Calls);
    }

    [Theory]
    [MemberData(nameof(MissingLibraryErrors))]
    public async Task ReadAsync_PrimaryMissing_RetriesOnFallbackAndStaysThere(Exception missing)
    {
        var primary = new StubPowerService { Error = missing };
        var fallback = new StubPowerService();
        var service = new PowerService(primary, fallback);

        await service.ReadAsync(CancellationToken.None);
        await service.ReadAsync(CancellationToken.None);
        await service.WriteAsync(new CpuLimit(80), CancellationToken.None);

        Assert.Equal(1, primary.Calls);
        Assert.Equal(3, fallback.Calls);
    }

    [Fact]
    public async Task WriteAsync_PrimaryMissing_RetriesOnFallback()
    {
        var primary = new StubPowerService { Error = new DllNotFoundException() };
        var fallback = new StubPowerService();

        await new PowerService(primary, fallback).WriteAsync(new CpuLimit(80), CancellationToken.None);

        Assert.Equal(1, fallback.Calls);
    }

    [Fact]
    public async Task ReadAsync_PrimaryErrorCode_RethrownWithoutFallback()
    {
        var primary = new StubPowerService { Error = new PowerControlException("PowerGetActiveScheme", 5) };
        var fallback = new StubPowerService();

        var error = await Assert.ThrowsAsync<PowerControlException>(
            () => new PowerService(primary, fallback).ReadAsync(CancellationToken.None));

        Assert.Equal(5, error.ErrorCode);
        Assert.Equal(0, fallback.Calls);
    }

    public static TheoryData<Exception> MissingLibraryErrors() =>
        [new DllNotFoundException(), new EntryPointNotFoundException()];

    private sealed class StubPowerService : IPowerService
    {
        public Exception? Error { get; init; }

        public int Calls { get; private set; }

        public Task<PowerState> ReadAsync(CancellationToken cancellationToken)
        {
            Calls++;

            return Error is null ? Task.FromResult(State) : Task.FromException<PowerState>(Error);
        }

        public Task WriteAsync(CpuLimit limit, CancellationToken cancellationToken)
        {
            Calls++;

            return Error is null ? Task.CompletedTask : Task.FromException(Error);
        }
    }
}
