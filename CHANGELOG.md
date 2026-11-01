# Changelog
## v0.1.0 - 2026-11-01

### SETUP-1
- ci: Align hooks, scripts and workflows with the new git strategy

### TW-1
- docs: Renumber task IDs

### TW-2
- ci: CI speed, no duplicate runs
- ci: Empty commit to test run cancel
- ci: Use throw in install-tools and check the dotnet exit code

### TW-3
- ci: Check PR title and base in pr-rules
- ci: Find sh in Git folder, restrict merge and hotfix bases

### TW-4
- ci: Review each head once, pass rulings, pin tool versions
- ci: Make rulings step safe under pipefail

### TW-5
- ci: Sync ubuntu first, shared setup-build action
- ci: Use the bot identity in the sync script test
- ci: Make the sync script test independent of git defaults

### TW-6
- ci: Test task order in CHANGELOG and release notes
- ci: Show git-cliff errors in the changelog test, share the lint loop

### Other
- chore: Add initial project files

