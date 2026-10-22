using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Tests.Fakes;

internal sealed class FakePowerService : IPowerService
{
    private int _concurrentWrites;
    private int _maxConcurrentWrites;
    private int _readCount;

    public PowerState State { get; set; } = new(CpuLimit.Full, CpuLimit.Full);

    public PowerControlException? ReadError { get; set; }

    public PowerControlException? WriteError { get; set; }

    /// <summary>Thrown by ReadAsync to prove that exceptions other than PowerControlException are not caught.</summary>
    public Exception? OtherReadError { get; set; }

    public List<CpuLimit> Writes { get; } = [];

    public int ReadCount => Volatile.Read(ref _readCount);

    /// <summary>Awaited inside WriteAsync, so a test can hold a write open.</summary>
    public Func<Task>? BeforeWrite { get; set; }

    public int MaxConcurrentWrites => Volatile.Read(ref _maxConcurrentWrites);

    public Task<PowerState> ReadAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _readCount);

        if (OtherReadError is not null)
        {
            throw OtherReadError;
        }

        return ReadError is not null ? throw ReadError : Task.FromResult(State);
    }

    public async Task WriteAsync(CpuLimit limit, CancellationToken cancellationToken)
    {
        TrackWriteStarted();

        try
        {
            await (BeforeWrite?.Invoke() ?? Task.CompletedTask);

            if (WriteError is not null)
            {
                throw WriteError;
            }

            Writes.Add(limit);
            State = new PowerState(limit, limit);
        }
        finally
        {
            Interlocked.Decrement(ref _concurrentWrites);
        }
    }

    private void TrackWriteStarted()
    {
        var now = Interlocked.Increment(ref _concurrentWrites);
        int seen;

        do
        {
            seen = Volatile.Read(ref _maxConcurrentWrites);
        }
        while (now > seen && Interlocked.CompareExchange(ref _maxConcurrentWrites, now, seen) != seen);
    }
}
