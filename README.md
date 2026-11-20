# Site Switcher

Site Switcher is a plugin for [N.I.N.A. (Nighttime Imaging 'N' Astronomy)](https://nighttime-imaging.eu/) that keeps a reusable list of observing locations and applies one to the active imaging profile with a single click.

## The idea

Imagers who travel, or who use more than one rig at a fixed location, repeatedly enter the same latitude, longitude and elevation into NINA's profile settings. Site Switcher removes that repetition. You save each location once — with a name, coordinates, elevation and optional notes — and then choose the one you are using for the current session.

Choosing a site updates the active NINA profile through NINA's own profile service, so the change behaves exactly like editing the latitude and longitude fields in Options → General. NINA raises its `LocationChanged` event and saves the profile as usual. The selected site is remembered per profile, so switching between profiles does not lose the association.

## Where it lives in NINA

NINA does not expose a public hook for adding controls to its Options → General profile page. Site Switcher therefore provides two entry points:

- A **Settings** tab on the plugin's entry under Options → Plugins, used to manage the saved site list and apply a site.
- A small **Site Switcher dock panel**, for changing location without leaving the imaging workspace.

Both views work against the same saved data and stay in sync.

## Features

- Named observing sites with latitude, longitude, elevation and notes.
- Coordinate validation: latitude −90 to 90, longitude −180 to 180.
- Create a site from the active profile's current coordinates.
- Apply a site to the active profile; lat/long/elevation update immediately and are persisted by NINA.
- Per-profile site selection.
- Shared JSON storage under NINA's plugin settings folder, independent of the deployed binaries.

## How it is put together

- `Plugin.cs` is the MEF-exported manifest and lifecycle owner.
- `Models/ObservingSite.cs` holds site data, validation and copy behaviour.
- `Models/SiteCatalog.cs` persists the shared site list as JSON and notifies the UI when it changes. A single shared instance is used because NINA composes plugin parts in separate MEF containers.
- `Services/SiteProfileService.cs` owns the per-profile selection and coordinates the updates to NINA's `IProfileService`.
- `Features/Settings/` contains the plugin options page.
- `Features/Dock/` contains the dockable picker.
- `tests/NinaSites.Tests/` covers metadata, persistence, profile updates and MEF composition.

## Compatibility and distribution

Site Switcher targets NINA 3.3.0.1060 or later and .NET 10. Its plugin GUID is permanent, so updates preserve saved sites and profile selections. It is distributed as a NINA plugin archive and catalog manifest through the standard NINA plugin repository.

## License

MPL-2.0. See `LICENSE.txt`.
