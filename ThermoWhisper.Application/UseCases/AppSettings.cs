using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.UseCases;

/// <summary>The user settings the UI shows. Not the CPU limit: the system is its source of truth.</summary>
public sealed record AppSettings(CpuLimit QuietProfile);
