## [0.1.0] - 2026-10-23

### 🚀 Features

- TW-2 Add CpuLimit and PowerState value objects (#29)
- TW-3 Add Application contract and test fakes (#31)
- TW-5 Add ReadPowerState, ApplyCpuLimit and ToggleSlowDown use cases (#32)
- TW-6 Add powrprof power service and Infrastructure.Tests (#33)
- TW-7 Add system compatibility probe (#35)
- TW-30 Add powercfg fallback and composite PowerService (#34)
- TW-27 Add ChangeQuietProfile, RestoreSpeedOnExit and LoadSettings use cases (#36)
- TW-4 Add CheckCompatibility startup check use case (#39)

### 🐛 Bug Fixes

- TW-30 Run Infrastructure tests one at a time so write tests do not race (#37)

### 📚 Documentation

- TW-29 Add product docs, README, privacy and third-party notices (#30)

### ⚙️ Miscellaneous Tasks

- Set up repository, CI and skeleton (#1)
- Clean skeleton, MSIX identity, final architecture.md (#28)
- Pre-1.0 tag builds are installable unsigned test packages (TW-32) (#38)
