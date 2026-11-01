#!/bin/sh
# Sets the version $1 (for example 2.18.0) in the project files (section 12 of GIT STRATEGY).
# ThermoWhisper: the only version in the files is ApplicationDisplayVersion of the MAUI project.
# The MSIX version becomes $1.0 (ApplicationVersion = 0, docs/architecture.md section 9).
set -e
file=ThermoWhisper/ThermoWhisper.csproj
grep -q '<ApplicationDisplayVersion>' "$file" || { echo "set-version: <ApplicationDisplayVersion> not found in $file" >&2; exit 1; }
sed -i "s|<ApplicationDisplayVersion>.*</ApplicationDisplayVersion>|<ApplicationDisplayVersion>$1</ApplicationDisplayVersion>|" "$file"
grep -q "<ApplicationDisplayVersion>$1</ApplicationDisplayVersion>" "$file" || { echo "set-version: failed to set $1" >&2; exit 1; }
