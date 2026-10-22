# Third-party notices

ThermoWhisper is licensed under Apache-2.0 (see `LICENSE` and `NOTICE`). The components below are shipped in the app
package (MSIX). Each license was checked on the official package page or the project's license file.
Test-only packages are not shipped and are not listed. This version lists the direct NuGet packages from
`Directory.Packages.props` and the bundled assets; transitive packages that the MSIX also ships are not listed yet.
Versions of the .NET runtime and Windows App SDK are set at build time, not in the repository.

| Component | Version | License | License and source |
|---|---|---|---|
| CommunityToolkit.Maui (NuGet) | 15.0.1 | MIT | https://licenses.nuget.org/MIT - https://github.com/CommunityToolkit/Maui |
| CommunityToolkit.Mvvm (NuGet) | 8.4.2 | MIT | https://licenses.nuget.org/MIT - https://github.com/CommunityToolkit/dotnet |
| H.NotifyIcon.Maui (NuGet) | 2.4.1 | MIT | https://licenses.nuget.org/MIT - https://github.com/HavenDV/H.NotifyIcon |
| Microsoft.Maui.Controls (NuGet) | 10.0.110 | MIT | https://licenses.nuget.org/MIT - https://github.com/dotnet/maui |
| Microsoft.Extensions.Logging.Debug (NuGet) | 10.0.12 | MIT | https://licenses.nuget.org/MIT - https://github.com/dotnet/dotnet |
| .NET runtime (self-contained app) | version set by the .NET 10 SDK at build time | MIT | https://github.com/dotnet/runtime/blob/main/LICENSE.TXT |
| Windows App SDK (self-contained app, pulled in by Microsoft.Maui.Controls) | version set at build time, not pinned in `Directory.Packages.props` | MIT | https://github.com/microsoft/WindowsAppSDK/blob/main/LICENSE |
| Open Sans fonts `Resources/Fonts/OpenSans-Regular.ttf` and `OpenSans-Semibold.ttf` | Open Sans (Google Fonts) | SIL Open Font License 1.1 | https://github.com/googlefonts/opensans/blob/main/OFL.txt - Copyright 2020 The Open Sans Project Authors |
| MAUI project template artwork `Resources/AppIcon/appicon.svg`, `Resources/AppIcon/appiconfg.svg`, `Resources/Splash/splash.svg` (placeholder until the final logo) | Microsoft.Maui.Templates 6.0.312 (files identical to the package) | MIT | https://licenses.nuget.org/MIT - https://github.com/dotnet/maui |
