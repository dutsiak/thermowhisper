# Glossary

| Term | Meaning |
|---|---|
| Quiet profile | The CPU % chosen by the user in Settings (Settings dropdown 10-50 in steps of 10, default 10) that Slow Down applies. One value, no presets in v1.0. |
| Slow Down | Command: set the maximum processor state (AC and DC) of the active power scheme to the quiet profile. |
| Restore Speed | Command: set the maximum processor state (AC and DC) of the active power scheme to 100 %. Always 100 %, never the previous value. |
| Max processor state | Windows power setting that caps CPU speed in percent; a lower cap means less heat and fan noise. |
| AC / DC | Plugged in / on battery. The app always sets the same value for both. |
| Active power scheme | The Windows power plan in use now. The app changes only this one. |
| Current limit | The value read from the system now. The system is the source of truth; the app never stores it. |
| Slowed state | The current limit is below 100 %. Shown by the slowed tray icon and the tooltip "CPU limit: N %"; otherwise "Full speed". |
| Compatibility check | Startup read-only probe that tells whether the app can change power settings. |
| Limited mode | The app runs but cannot change power settings (check failed); only Settings view, donation, Close and Exit work. |
| External change | The limit was changed by another tool or the user; the app shows it and does not fight back. |
| Autostart | Start the app with Windows (hidden in the tray). Does not apply Slow Down. |
| Donation reminder | One toast 7 days after first run; never again. |
| Tray app | The app lives in the notification area; closing Settings hides it; only Exit quits (and restores full speed). |
