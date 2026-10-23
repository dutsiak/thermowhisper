using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Power;

public sealed record PowerActionResult
{
    private PowerActionResult(PowerState? state, string? errorKey, string? errorDetail)
    {
        State = state;
        ErrorKey = errorKey;
        ErrorDetail = errorDetail;
    }

    public bool IsSuccess => ErrorKey is null;

    /// <summary>Fresh state read during the action; null if none or failed.</summary>
    public PowerState? State { get; }

    /// <summary>A <see cref="TextKeys"/> constant.</summary>
    public string? ErrorKey { get; }

    /// <summary>Technical detail, for example the exception message.</summary>
    public string? ErrorDetail { get; }

    public static PowerActionResult Success(PowerState? state = null) => new(state, null, null);

    public static PowerActionResult Failure(string errorKey, string? errorDetail = null) =>
        new(null, errorKey, errorDetail);
}
