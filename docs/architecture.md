# ThermoWhisper architecture
Windows tray app that makes a laptop quieter by lowering the CPU "maximum processor state" of the active power
scheme. .NET 10, MAUI (Windows only), self-contained MSIX. Read only the section you need.
- Stack (fixed by the PO): .NET 10 + MAUI; power control via the native powrprof API (typed values and return codes).
  Rejected as the only path: `powercfg.exe` (a process per call, localized console text); it stays as the fallback.
- Build/verify: CLAUDE.md "Build and verify". Product and terms: `docs/product.md` (UC-1..UC-8), `docs/domain/`.

## 1. Solution layout (flat on disk)
| Project | TFM | Contents |
|---|---|---|
| `ThermoWhisper.Domain` | net10.0 | Value objects `CpuLimit` (1-100), `PowerState`. No dependencies. |
| `ThermoWhisper.Application` | net10.0 | **All interfaces** + use cases. Refs Domain only. |
| `ThermoWhisper.Infrastructure` | net10.0 | powrprof P/Invoke, powercfg fallback, compatibility probe. No NuGet packages. |
| `ThermoWhisper` (the "App") | net10.0-windows10.0.19041.0 | MAUI: views, view models, tray, localization, DI, WinRT/MAUI implementations. |
| `ThermoWhisper.Domain.Tests`, `ThermoWhisper.Application.Tests`, `ThermoWhisper.Infrastructure.Tests` (created by TW-6) | net10.0 | xUnit; fakes for interfaces. |

- `src` and `tests` are **solution folders only** (in `ThermoWhisper.slnx`), not folders on disk. New projects go in
  the repo root: `dotnet sln ThermoWhisper.slnx add <proj> --solution-folder src|tests`. Test projects: `*.Tests`.
- Dependencies: `App -> Infrastructure -> Application -> Domain`, never the other way. Interfaces only in Application.

## 2. Layers
- **Domain**: `CpuLimit` - immutable, valid 1-100, throws on invalid input; `CpuLimit.Full` = 100.
  `PowerState(Ac, Dc)`: `IsSlowed` = AC or DC below 100; `Shown` = the lower one (differ only after an external change).
- **Application**: interfaces `IPowerService` (read AC and DC of the active scheme; write one value to both and
  re-activate the scheme), `IAutostartService`, `ISettingsStore`, `ISecureSettingsStore`, `IDonationReminder`,
  `ILocalizer`, `IWindowService`, `IDialogService`, `ISystemCompatibilityProbe`. Use cases: `CheckCompatibility`,
  `ReadPowerState`, `ApplyCpuLimit`, `ToggleSlowDown`, `ChangeQuietProfile`, `RestoreSpeedOnExit`, `LoadSettings`
  (section 5), donation schedule (section 7). Singletons: `SystemSupport` (startup check result), `PowerStatus`
  (last read `PowerState` + `Changed` event; memory only, for display). No Windows/MAUI types.
- **Errors**: Infrastructure throws `PowerControlException` (declared in Application: failed call + code). Use cases
  catch only it and return `PowerActionResult` (success, or error text key + detail); view models show it (tray: one
  `IDialogService` popup; Settings: status line). Other exceptions are bugs and are not caught.
- **Infrastructure** (no WinRT/MAUI API, `[SupportedOSPlatform("windows")]`): `Power/`, `Compatibility/`. No MAUI
  packages: `Microsoft.Maui.Essentials` pulls Win2D and breaks an AnyCPU library (WIN2D0001).
- **App**: `MauiInfrastructure/` - `Autostart/` (StartupTask), `Notifications/` (AppNotificationManager toast),
  `SettingsMauiStorage/` (Preferences, SecureStorage); `Platforms/Windows/` - tray, window, dialogs, single instance.

## 3. UI rules
- MVVM: CommunityToolkit.Mvvm `[ObservableProperty]`, `[RelayCommand]`; CommunityToolkit.Maui
  (`.UseMauiCommunityToolkit()`); DI only in `MauiProgram.cs`. **No code-behind logic**: a `.xaml.cs` has only
  `InitializeComponent()` and a constructor that gets the view model from DI and sets `BindingContext`.
- Async load: `EventToCommandBehavior` (NavigatedTo -> `LoadCommand`), page inside `AppShell`. Settings is hidden, not
  closed, so NavigatedTo fires once: the tray "Settings" command calls `SettingsViewModel.LoadAsync()` on every show.
- Tray (H.NotifyIcon.Maui `TaskbarIcon`; setup, icon format, events only from its README/source), bound to
  `TrayViewModel`: one toggling item (Slow Down / Restore Speed when slowed), Settings, Exit. Tooltip "CPU limit: N %"
  (N = `Shown`) or "Full speed"; two icons (normal / slowed) by `IsSlowed` of the real limit, never an app flag.
- **State refresh** (`ReadPowerState` -> `PowerStatus.Changed` -> tray and Settings update on the UI thread): at start,
  when the tray menu opens, after every write, every 3 min while Settings is visible (`PeriodicTimer` +
  `CancellationToken` in the Settings view model: start on show, stop on hide). Between reads the tray may be stale.
- **Start** (user, autostart, or second launch) only reads and shows the state; never slows down by itself.
  Single instance: `AppInstance.FindOrRegisterForKey`; a second launch redirects to the first and exits.
  A start from the Start menu and a second launch open the Settings window; an autostart launch stays in the tray
  only. The quiet-profile dropdown offers 10-50 % in steps of 10; the default is 10 % (all PO 2026-10-23).
- Closing Settings = hide (`IWindowService`); only tray "Exit" quits (section 5).
- Text only from `.resx` (`App/Localization/AppResources.resx` = English, `AppResources.uk.resx`, ...), from the first
  UI task on. Adding a language = one `.resx` file + one entry in the language list.

## 4. Startup compatibility check
Runs once at start, after the single-instance check, before the tray menu is usable. Never writes, never throws: any
exception = a listed problem. `ISystemCompatibilityProbe` returns raw facts; `CheckCompatibility` (unit-tested)
returns `CompatibilityResult { IsSupported, Problems[] }`, kept in `SystemSupport`. Checks: (a) Windows >= 10.0.17763;
(b) `powrprof.dll` loads and exports PowerGetActiveScheme, PowerReadACValueIndex, PowerReadDCValueIndex,
PowerWriteACValueIndex, PowerWriteDCValueIndex, PowerSetActiveScheme (`NativeLibrary.TryLoad` / `TryGetExport`);
(c) `%SystemRoot%\System32\powercfg.exe` exists; (d) the active scheme can be read and has the max-processor-state
setting - checked by `CheckCompatibility` through the `IPowerService` read, so it tests the path in use.
- Blocked if (a) fails, or (b) and (c) both fail, or (d) fails. Only (b) fails -> no popup, powercfg fallback.
- Blocked -> one localized popup (`IDialogService`) listing the problems, then **limited mode**: greyed tray Slow
  Down/Restore, Settings dropdown, autostart, Apply (`CanExecute` / `IsEnabled`); active: tray Settings and Exit,
  donation, Close; Settings status line with the problem text; tooltip "Limited mode", normal icon. Use cases also
  refuse writes (not only the UI); Exit quits without restoring. All fine -> say nothing.
- A write that fails later (e.g. blocked by policy) is an error message, not limited mode.

## 5. Power control
- Subgroup `54533251-82be-4824-96c1-47b60b740d00`, setting (max processor state) `bc5038f7-23e0-4960-96da-33abaf5935ec`.
- Primary: powrprof `PowerGetActiveScheme` (free with `LocalFree`), `PowerRead/WriteACValueIndex`,
  `PowerRead/WriteDCValueIndex`, then `PowerSetActiveScheme`. Check every return code (0 = success).
  `IPowerService` is async; the composite switches to powercfg for the rest of the run only when powrprof.dll or an
  export is missing (DllNotFound / EntryPointNotFound); a powrprof error code is an error, not a fallback.
- Same value for AC and DC. Never hardcode a scheme GUID - read the active one; change only the active scheme.
- Fallback: `powercfg /q SCHEME_CURRENT <subgroup> <setting>` (parse only the two hex values, never localized labels),
  `/setacvalueindex`, `/setdcvalueindex`, `/setactive SCHEME_CURRENT`; `CreateNoWindow`, check `ExitCode`, wait at
  most 10 s, then kill = error; redirect output only for the read. Calls run in `Task.Run`; UI updates on UI thread.
- **The system is the source of truth** for the CPU limit and autostart state: never stored, always read.
  `PowerStatus` is only the last read for display; every command below reads the system first.

Commands (UC = `docs/product.md`). Every write goes through `ApplyCpuLimit`: refused in limited mode, one at a time
(`SemaphoreSlim`), AC and DC together, then re-read and update `PowerStatus`.
- Slow Down (UC-1): the quiet profile % from `ISettingsStore` (default 10). Restore Speed (UC-2): always 100 %.
- `ToggleSlowDown`: fresh read; slowed -> Restore Speed, else Slow Down.
- `ChangeQuietProfile` (UC-3, Settings Apply): saves the %; if slowed now, applies the new % at once.
- `RestoreSpeedOnExit` (UC-5): limited mode -> skip; fresh read; slowed -> write 100 (even if another tool set it);
  on error one popup, then quit anyway.
- Crash, kill, shutdown or sign-out while slowed: nothing runs, the CPU stays slowed - accepted for v1.0 (no shutdown
  handler); the next start shows the slowed state (UC-4). External change: shown at the next read, never written back.

## 6. Settings storage
- `ISettingsStore` - MAUI `Preferences.Default`, always `sharedName: "ThermoWhisper"`. Keys: quiet-profile %, UI language.
- `ISecureSettingsStore` - MAUI `SecureStorage.Default` (values the user must not edit easily): `FirstRunUtc` (written
  once), `DonationToastShown`, `DonationUrl` (placeholder `https://google.com` written on first run if absent; always read from here).

## 7. Autostart and donation reminder
- Autostart: `StartupTask` in `Package.appxmanifest` + `Windows.ApplicationModel.StartupTask` API, TaskId
  `ThermoWhisperStartup`. `DisabledByUser` / `DisabledByPolicy`: show the state, do not fight the user.
- Donation toast (`AppNotificationManager`, Windows App SDK comes with MAUI), "Donate" button opens `DonationUrl`.
  Shown **once ever**: `now >= FirstRunUtc + 7 days` and `DonationToastShown != true`, then set the flag. Checked at
  start and every hour; logic in Application with `TimeProvider` (unit-tested). Reinstall restarts the week - accepted.

## 8. Build configuration - one value, one place
| Setting | Only place |
|---|---|
| .NET SDK version | `global.json` |
| Nullable, ImplicitUsings, warnings | `Directory.Build.props` (not in .csproj) |
| NuGet versions | `Directory.Packages.props`; .csproj has `<PackageReference Include="X" />` without `Version` |
| Which packages a project uses; TFM, UseMaui, MSIX/app properties | that project's .csproj |
| App version | `<ApplicationDisplayVersion>` in `ThermoWhisper/ThermoWhisper.csproj`, set only by `scripts/release/set-version.sh` |

## 9. Packaging and release
- `WindowsPackageType=MSIX`, `SelfContained`, `WindowsAppSDKSelfContained`, `RuntimeIdentifier=win-x64`: only in the
  App .csproj, under the Windows TFM condition. `test.ps1` overrides `WindowsPackageType=None`; `build.ps1` passes
  only output/signing switches. Local run: add `-p:WindowsPackageType=None` (a packaged exe cannot start bare);
  Visual Studio F5 "Windows Machine" deploys the package. Min Windows 10.0.17763.0 (`TargetPlatformMinVersion`),
  target 10.0.19041.0. No trimming for now. MSIX version = `ApplicationDisplayVersion` (3 parts) +
  `ApplicationVersion` `0` -> `1.2.3.0`.
- `Platforms/Windows/Package.appxmanifest`: keep `Identity Version="0.0.0.0"` (MAUI fills it), the `$placeholder$`
  values and `rescap:runFullTrust`. `Identity Name` = `Dutsiak.ThermoWhisper` (MAUI fills the placeholder with
  `ApplicationIdGuid`, not `ApplicationId`); `PublisherDisplayName` = Markiian Dutsiak. `Identity Publisher` = the
  signing certificate subject exactly (`CN=User Name` until SignPath approval). Test manifest changes by installing.
- Signing only in CI by SignPath (`.signpath/artifact-configuration.xml` = reference copy); never sign locally with that certificate; local MSIX uses the VS dev certificate.
- Tag `vX.Y.Z` -> CI builds, signs, makes a **draft** GitHub Release; manual `deploy-prod` publishes it (the production deploy).
  Tests that change the power scheme: say the value first, restore the original afterwards.

## 10. Test builds (dev / stage)
Tell the PO when a test build is ready. Every push to `dev` / `stage` (after lint and tests) builds an unsigned **test** MSIX; PRs build none.
- Download: github.com -> repo -> Actions -> the run of that push -> Artifacts -> `ThermoWhisper-<branch>-<run>-unsigned-test`
  (zip with the .msix). Needs GitHub sign-in; kept 30 days.
- Install (Windows 11 only, no double-click), PowerShell **as administrator**: `Add-AppxPackage -Path .\<file>.msix -AllowUnsigned`.
- Why it installs: for these builds only, `scripts/ci/build.ps1` (`UNSIGNED_TEST=true`) appends Microsoft's test marker
  `OID.2.25.311729368913984317654407730594956997722=1` to `Identity Publisher` (repo manifest unchanged; release builds
  never get it). Source: https://github.com/microsoftdocs/msix-docs/blob/main/msix-src/package/unsigned-package.md
- Limits: testing only. A test install is a separate app from the signed release (different identity, settings not
  shared). Uninstall: Settings -> Apps.

## 11. Licenses
Apache-2.0 (`LICENSE`, `NOTICE`); names and logo - `TRADEMARKS.md`. Every new NuGet package, icon, font or image: check its
license, add it to `THIRD-PARTY-NOTICES.md` in the same change. Allowed: MIT, Apache-2.0, BSD, OFL, CC0/CC-BY; else ask the PO.

## 12. Risky places
- Writes from the installed MSIX as a standard (non-admin) user: verify on real hardware (TW-6, TW-23). Group Policy,
  Windows 11 "Power mode" or OEM tools may block, override or reset the value - we only show what the scheme says.
- MAUI always creates a main window: starting hidden in the tray needs Windows platform code; Settings reuses it.
