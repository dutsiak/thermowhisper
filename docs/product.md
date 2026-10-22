# ThermoWhisper - product brief

## Product
- Problem: laptop fans get loud during long demanding tasks, and Windows hides the setting that fixes it ("maximum processor state"). Users need silence quickly, without digging through Power Options.
- Promise: "Make your laptop quiet with one click."
- Users: owners of Windows 11 laptops who need silence: at night with someone sleeping in the room; heavy tasks left running overnight; audio recording with a sensitive condenser microphone that picks up fan noise. Not experts in power management.
- Main scenarios:
  - UC-1 Slow Down from the tray: limit (AC and DC) set to the quiet profile; tooltip and icon show it.
  - UC-2 Restore Speed from the tray: limit set to 100 % (AC and DC).
  - UC-3 Choose the quiet profile % in Settings (default 10).
  - UC-4 See the real current state (tooltip, Settings), even if another tool changed it.
  - UC-5 Exit from the tray: full speed restored, app closes.
  - UC-6 Autostart with Windows (does not slow the CPU by itself). A start from the Start menu or a second launch opens the Settings window; an autostart launch stays in the tray.
  - UC-7 Limited mode: compatibility check fails, one popup, writing disabled.
  - UC-8 One donation reminder 7 days after first run.
- MVP (v1.0): in - tray with Slow Down / Restore Speed toggle, tooltip and two icons, Settings window (quiet profile %, current limit, autostart, language English/Ukrainian, donation banner), restore to 100 % on Exit, limited mode, single instance, signed MSIX on GitHub Releases. Out - presets, schedules, auto-switch by battery or app, temperature/fan graphs, per-app rules, other OS, auto-update, Microsoft Store, telemetry.
- Platforms and constraints: Windows 11 (Windows 10 1809+, minimum 10.0.17763, nice to have), laptops; .NET 10 + MAUI; offline, no telemetry; free, Apache-2.0, public GitHub repo; the system is the source of truth for limit and autostart.
- UI: simple tray menu (Slow Down / Restore Speed, Settings, Exit) and one Settings window in Windows 11 style.
- Success (v1.0): installs on a clean Windows 11; Slow Down and Restore change the AC and DC values shown in Windows Power Options.
- Epics / releases (match PLAN.md sprints):
  1. v0.1.0 Core logic and power control (TW-1..TW-7, TW-27, TW-29, TW-30) - medium
  2. v0.2.0 Tray app shell, single instance, limited mode (TW-8..TW-12, TW-15, TW-28) - medium
  3. v0.3.0 Settings window (TW-13, TW-14, TW-16) - medium
  4. v0.4.0 Autostart and languages (TW-17, TW-18) - small
  5. v0.5.0 Donation reminder (TW-19..TW-21) - small
  6. v0.6.0 Packaging, icon, clean-VM install test (TW-22, TW-23) - small
  7. v1.0.0 Signing and first public release (TW-24..TW-26) - small
- ASSUMPTION: crash, kill or shutdown while slowed leaves the CPU slowed; acceptable for v1.0 (user clicks Restore Speed).
- ASSUMPTION: changing the quiet profile while slowed applies the new % at once; otherwise it is only saved.
- ASSUMPTION: the slowed icon and tooltip follow the real limit (below 100 %), not an app flag.
