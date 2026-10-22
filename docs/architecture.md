# ThermoWhisper architecture

Windows tray app that makes a laptop quieter by lowering the CPU "maximum processor state"
of the active power scheme. .NET 10, MAUI (Windows only), self-contained MSIX.
Read only the section you need.

## 1. Solution layout (flat on disk)

| Project | TFM | Contents |
|---|---|---|
| `ThermoWhisper.Domain` | net10.0 | `CpuLimit` value object (1-100). No dependencies. |
| `ThermoWhisper.Application` | net10.0 | **All interfaces** + use cases. Refs Domain only. |
| `ThermoWhisper.Infrastructure` | net10.0 | powrprof P/Invoke, powercfg fallback, compatibility probe. No NuGet packages. |
| `ThermoWhisper` (the "App") | net10.0-windows10.0.19041.0 | MAUI: views, view models, tray, localization, DI, WinRT/MAUI implementations. |
| `ThermoWhisper.Domain.Tests`, `ThermoWhisper.Application.Tests` | net10.0 | xUnit; fakes for interfaces. |

- `src` and `tests` are **solution folders only** (in `ThermoWhisper.slnx`), not folders on disk.
  New projects go in the repo root: `dotnet sln ThermoWhisper.slnx add <proj> --solution-folder src|tests`.
- Test projects are named `*.Tests` - CI finds them by that name.
- Dependencies: `App -> Infrastructure -> Application -> Domain`. Never the other way.
- Interfaces are declared only in Application. Infrastructure and App contain only classes.

## 2. Layers

- **Domain**: `CpuLimit` - immutable, valid 1-100, throws on invalid input.
- **Application**: interfaces `IPowerService`, `IAutostartService`, `ISettingsStore`, `ISecureSettingsStore`,
  `IDonationReminder`, `ILocalizer`, `IWindowService`, `IDialogService`, `ISystemCompatibilityProbe`;
  use cases `ApplyCpuLimit`, `LoadSettings`, `ToggleSlowDown`, `CheckCompatibility`. No Windows/MAUI types.
- **Infrastructure** (no WinRT/MAUI API, classes marked `[SupportedOSPlatform("windows")]`):
  `Power/` (`IPowerService`), `Compatibility/` (`ISystemCompatibilityProbe`).
  No MAUI packages: `Microsoft.Maui.Essentials` pulls Win2D and breaks an AnyCPU library (WIN2D0001).
- **App**:
  - `MauiInfrastructure/Autostart/` - `IAutostartService` (StartupTask).
  - `MauiInfrastructure/Notifications/` - `IDonationReminder` (AppNotificationManager toast).
  - `MauiInfrastructure/SettingsMauiStorage/` - `ISettingsStore` (Preferences), `ISecureSettingsStore` (SecureStorage).
  - `Platforms/Windows/` - tray, `IWindowService`, `IDialogService`, single instance.

## 3. UI rules

- MVVM: CommunityToolkit.Mvvm `[ObservableProperty]`, `[RelayCommand]`; CommunityToolkit.Maui
  (`.UseMauiCommunityToolkit()`). DI only in `MauiProgram.cs`.
- **No code-behind logic**: a `.xaml.cs` has only `InitializeComponent()` and a constructor that gets the
  view model from DI and sets `BindingContext`.
- Async load: `EventToCommandBehavior` (NavigatedTo -> `LoadCommand`); page inside `AppShell`.
  The Settings window is hidden, not closed, so NavigatedTo fires once: the tray "Settings" command
  calls `SettingsViewModel.LoadAsync()` every time it shows the window.
- While Settings is visible, re-read the system every 3 minutes (`PeriodicTimer` + `CancellationToken`,
  in the view model or a service it owns): start on show, stop on hide.
- Tray (H.NotifyIcon.Maui `TaskbarIcon`, check its README for MAUI setup): menu bound to `TrayViewModel`:
  Slow Down / Restore Speed (one toggling item), Settings, Exit.
- Closing Settings = hide (`IWindowService`). Only tray "Exit" quits.
- Single instance: `AppInstance.FindOrRegisterForKey`; a second launch redirects to the first and exits.
- Text only from `.resx` (`App/Localization/AppResources.resx` = English, `AppResources.uk.resx`, ...).
  Adding a language = one `.resx` file + one entry in the language list.

## 4. Startup compatibility check

Runs once at start, after the single-instance check, before the tray menu is usable. Never writes, never throws:
any exception = a listed problem. `ISystemCompatibilityProbe` returns raw facts; `CheckCompatibility`
(unit-tested) returns `CompatibilityResult { IsSupported, Problems[] }`.

Checks: (a) Windows >= 10.0.17763; (b) `powrprof.dll` loads and exports PowerGetActiveScheme,
PowerReadACValueIndex, PowerReadDCValueIndex, PowerWriteACValueIndex, PowerWriteDCValueIndex,
PowerSetActiveScheme (`NativeLibrary.TryLoad` / `TryGetExport`); (c) `%SystemRoot%\System32\powercfg.exe`
exists; (d) the active scheme can be read and has the max-processor-state setting.

- Blocked if (a) fails, or (b) and (c) both fail, or (d) fails.
- Only (b) fails -> no popup, use the powercfg fallback.
- All fine -> say nothing.
- Blocked -> one localized popup (`IDialogService`) listing the problems, then **limited mode**:
  a shared `IsSystemSupported = false`; greyed: tray Slow Down/Restore, Settings dropdown, autostart, Apply
  (`CanExecute` / `IsEnabled` bindings); still active: tray Settings and Exit, donation, Close;
  Settings shows a status line with the problem text.

## 5. Power control

- Subgroup `54533251-82be-4824-96c1-47b60b740d00`, setting (max processor state) `bc5038f7-23e0-4960-96da-33abaf5935ec`.
- Primary: powrprof `PowerGetActiveScheme` (free with `LocalFree`), `PowerRead/WriteACValueIndex`,
  `PowerRead/WriteDCValueIndex`, then `PowerSetActiveScheme`. Check every return code (0 = success).
- Same value for AC and DC. Never hardcode a scheme GUID - read the active one.
- Fallback: `powercfg /setacvalueindex`, `/setdcvalueindex`, `/setactive SCHEME_CURRENT`; `CreateNoWindow`,
  check `ExitCode`; no `RedirectStandardOutput` unless the output is read (can hang).
- Calls run in `Task.Run`; UI updates on the UI thread.
- **The system is the source of truth** for the CPU limit and autostart state: never stored, always read.

## 6. Settings storage

- `ISettingsStore` - MAUI `Preferences.Default`, always `sharedName: "ThermoWhisper"`. Keys: quiet-profile %, UI language.
- `ISecureSettingsStore` - MAUI `SecureStorage.Default` (values the user must not edit easily):
  `FirstRunUtc` (written once), `DonationToastShown`, `DonationUrl` (default `https://google.com` written on
  first run if absent; placeholder). The app always reads the URL from this store.

## 7. Autostart and donation reminder

- Autostart: `StartupTask` in `Package.appxmanifest` + `Windows.ApplicationModel.StartupTask` API, TaskId
  `ThermoWhisperStartup`. `DisabledByUser` / `DisabledByPolicy`: show the state, do not fight the user.
- Donation toast (`AppNotificationManager`, Windows App SDK comes with MAUI) with a "Donate" button that opens
  `DonationUrl`. Shown **once ever**: when `now >= FirstRunUtc + 7 days` and `DonationToastShown != true`,
  then set the flag. Checked at start and every hour. Logic in Application with `TimeProvider` (unit-tested).
  Reinstall clears app data, so the week starts again - accepted.

## 8. Build configuration - one value, one place

| Setting | Only place |
|---|---|
| .NET SDK version | `global.json` |
| Nullable, ImplicitUsings, warnings | `Directory.Build.props` (not in .csproj) |
| NuGet versions | `Directory.Packages.props`; .csproj has `<PackageReference Include="X" />` without `Version` |
| Which packages a project uses; TFM, UseMaui, MSIX/app properties | that project's .csproj |
| App version | `<ApplicationDisplayVersion>` in `ThermoWhisper/ThermoWhisper.csproj`, set only by `scripts/release/set-version.sh` |

## 9. Packaging and release

- `SelfContained=true`, `WindowsAppSDKSelfContained=true`, `RuntimeIdentifier=win-x64`, `WindowsPackageType=MSIX`.
  Min Windows 10.0.17763.0 (from `TargetPlatformMinVersion`), target 10.0.19041.0. No trimming for now.
- MSIX version = `ApplicationDisplayVersion` (3 parts) + `ApplicationVersion` = `0` -> `1.2.3.0`.
- `Platforms/Windows/Package.appxmanifest`: keep `Identity Version="0.0.0.0"` (MAUI fills it) and the
  `$placeholder$` values; keep `rescap:runFullTrust`. `Identity Publisher` must equal the signing certificate
  subject exactly - `CN=User Name` until SignPath approval. Manifest changes are tested by installing the MSIX.
- Signing only in CI by SignPath (`.signpath/artifact-configuration.xml` is the reference copy). Never sign
  locally with that certificate; local MSIX uses the Visual Studio dev certificate.
- Release tag `vX.Y.Z` -> CI builds the MSIX, signs it, creates a **draft** GitHub Release.
  Manual `deploy-prod` workflow publishes the draft - that is the "production deployment" of this app.
- Changing the power scheme during tests is a real system change: say what will be written and restore the
  original value afterwards.

## 10. Test builds (dev / stage)

Tell the PO this whenever a test build is ready to try.
- Every push to `dev` / `stage` (after lint and tests) builds an unsigned **test** MSIX, Release configuration.
  Pull requests build no MSIX.
- Download: github.com -> repo -> Actions -> the run of that push -> Artifacts ->
  `ThermoWhisper-<branch>-<run>-unsigned-test` (zip with the .msix). Needs GitHub sign-in; kept 30 days.
- Install (Windows 11 only): PowerShell **as administrator**:
  `Add-AppxPackage -Path .\<file>.msix -AllowUnsigned`. Double-click install does not work for unsigned packages.
- Why it installs: for these builds only, `scripts/ci/build.ps1` (`UNSIGNED_TEST=true`) appends Microsoft's test marker
  `OID.2.25.311729368913984317654407730594956997722=1` to `Identity Publisher`; without it Windows refuses an
  unsigned package. The manifest in the repo is not changed. Release builds never get the marker.
- Limits: testing only (Microsoft: do not distribute unsigned packages). A test install is a separate app from the
  signed release (different identity): both can be installed, settings are not shared. Uninstall: Settings -> Apps.
- Source: https://github.com/microsoftdocs/msix-docs/blob/main/msix-src/package/unsigned-package.md

## 11. Licenses

Apache License 2.0 (`LICENSE`, `NOTICE`). Names "ThermoWhisper", "Dutsiak" and the logo - `TRADEMARKS.md`.
Every new NuGet package, icon, font or image: check its license, add it to `THIRD-PARTY-NOTICES.md` in the
same change. Allowed: MIT, Apache-2.0, BSD, OFL (fonts), CC0/CC-BY (images); anything else - ask the PO.
