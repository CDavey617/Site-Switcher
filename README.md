# Site Switcher — N.I.N.A. plugin

A plugin for **N.I.N.A. 3.3.0.1060 or later** that stores a reusable list of observing sites and applies a chosen site's latitude, longitude and elevation to the active NINA profile.

NINA does not provide a public extension point for adding controls to its **Options → General** profile page. This plugin therefore exposes the site picker in two places:

1. **Options → Plugins → Site Switcher → Settings** — manage (add, edit, delete) sites and apply one to the active profile.
2. **Site Switcher dock panel** — a small dockable panel for switching sites without leaving the imaging screen.

## What it does

- Saves named sites with latitude (`-90` to `90`), longitude (`-180` to `180`), elevation and notes.
- Adds a site from the active profile's current coordinates with one click.
- Applies a site through `IProfileService.ChangeLatitude`, `ChangeLongitude` and `ChangeElevation`. NINA raises its `LocationChanged` event and saves the profile, exactly as if the values were edited in Options → General.
- Remembers the selected site **per NINA profile**, so switching profiles does not lose the choice.

## Where data is stored

- The shared site list: `%LOCALAPPDATA%\NINA\PluginSettings\9AE89050-4DDE-4F67-991A-3A6D34A90A80\sites.json`
- The selected site for each profile: in that profile's plugin settings under the same GUID.

Nothing is stored next to the plugin binaries, so updating the plugin does not erase sites.

## Build and deploy (Windows)

Prerequisites:

- Windows
- .NET SDK `10.0.4xx` (see `global.json`)
- PowerShell 7
- NINA 3.3.0.1060 or later

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

To deploy into your local NINA installation:

```powershell
Copy-Item local.props.example local.props
# Edit local.props and set NinaExecutable to your NINA.exe path
dotnet build ./src/NinaSites/NinaSites.csproj -c Release -t:DeployPlugin
```

Close NINA before deploying, then reopen it. The deploy target copies the plugin to:

```
%LOCALAPPDATA%\NINA\Plugins\3.0.0\Site Switcher\
```

In NINA, go to **Options → Plugins → Installed → Site Switcher** to manage sites, and enable the **Site Switcher** dock panel from the dock panel menu.

If you prefer to copy files manually, create that folder yourself and copy only these files from `src\NinaSites\bin\Release\net10.0-windows\`:

- `NinaSites.dll`
- `NinaSites.pdb` (optional)
- `LICENSE.txt` from the project root

Do not copy the `NINA.*.dll` files from the build output — NINA already provides those.

To produce a distributable ZIP and repository manifest:

```powershell
./build/Build-Release.ps1 -Tag v1.0.0.0 -Repository craigdavey/nina-sites-plugin
```

## Project layout

```
src/NinaSites/
  Plugin.cs                         Manifest and lifecycle
  Models/ObservingSite.cs           Site data + coordinate validation
  Models/SiteCatalog.cs             JSON persistence of the shared site list
  Services/SiteProfileService.cs    Per-profile selection + applying coordinates
  Features/Settings/                Options → Plugins settings page
  Features/Dock/                    Dockable site picker
tests/NinaSites.Tests/              NUnit + Moq tests
build/                              Staging, packaging and deployment targets
```

## License

MPL-2.0. See `LICENSE.txt`.
