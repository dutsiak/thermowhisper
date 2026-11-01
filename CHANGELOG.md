# Changelog
## v0.1.0 - 2026-11-01

### TW-1
- feat: (tray) Add tray menu with Slow Down / Restore Speed toggle and PowerActionGate
- feat: (tray) Register tray services and start the app from a lifecycle event
- feat: (tray) Read the power state on right mouse down via a behavior
- feat: (tray) Give the menu-open behavior the icon's binding context
- fix: (tray) Take the shell title from resources

### TW-2
- feat: (tray) Add Exit that restores full speed, and hide on close

### TW-3
- feat: (tray) Show the real CPU limit in the tray tooltip and icon

### TW-4
- feat: (tray) Show one limited-mode popup and grey out the toggle

### Other
- chore: Initial commit
- ci: Add check-base and move git checks to a Linux job
- ci: Make release and deploy run when git-checks is skipped
- ci: Add check-base test and fix the sync-main test pattern
- ci: Use the allowed bot identity in check-base-test

