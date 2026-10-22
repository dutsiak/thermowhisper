# ThermoWhisper

Make your laptop quiet with one click.

ThermoWhisper is a Windows 11 tray app. Slow Down lowers the CPU maximum processor state of the active power scheme to your quiet profile; Restore Speed sets it back to 100 %. It works offline.

[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

- [Privacy](PRIVACY.md)
- [Third-party notices](THIRD-PARTY-NOTICES.md)
- [Names and logo (TRADEMARKS)](TRADEMARKS.md)

## Development setup
Once per clone:
- Enable the git hooks: `git config core.hooksPath .githooks`
- Install gitleaks: `winget install --id Gitleaks.Gitleaks -e`
- Install Git LFS: `winget install --id GitHub.GitLFS -e`, then `git lfs install`
