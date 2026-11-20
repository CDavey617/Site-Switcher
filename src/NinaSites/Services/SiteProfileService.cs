using CraigDavey.Nina.Sites.Models;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CraigDavey.Nina.Sites.Services {
    /// <summary>
    /// Applies a saved site to the active NINA profile and remembers the chosen
    /// site per profile using NINA's plugin options accessor.
    /// </summary>
    [Export(typeof(SiteProfileService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class SiteProfileService : IDisposable {
        private const string SelectedSiteIdKey = "SelectedSiteId";

        /// <summary>
        /// Raised when a site is applied or the active profile changes. Static so
        /// that parts from separate MEF containers stay in sync.
        /// </summary>
        internal static event Action SelectionApplied;

        private readonly IProfileService profiles;
        private readonly PluginOptionsAccessor options;
        private bool initialized;

        [ImportingConstructor]
        public SiteProfileService(IProfileService profiles) {
            this.profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
            var pluginId = typeof(Plugin).Assembly.GetCustomAttribute<GuidAttribute>()?.Value;
            options = new PluginOptionsAccessor(profiles, Guid.Parse(pluginId));
        }

        public void Initialize() {
            if (initialized) {
                return;
            }

            profiles.ProfileChanged += OnProfileChanged;
            // Also keeps the panels in sync when coordinates are edited elsewhere.
            // Having a subscriber avoids a null-event edge case in NINA's ChangeLongitude.
            profiles.LocationChanged += OnLocationChanged;
            initialized = true;
        }

        public string ProfileName => profiles.ActiveProfile?.Name ?? "active profile";

        public string CurrentLocation {
            get {
                var astrometry = profiles.ActiveProfile?.AstrometrySettings;
                if (astrometry == null) {
                    return "No active profile";
                }

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Active profile: Lat {0:F5}, Lon {1:F5}, Elev {2:F0} m",
                    astrometry.Latitude,
                    astrometry.Longitude,
                    astrometry.Elevation);
            }
        }

        public string SelectedSiteId {
            get => options.GetValueString(SelectedSiteIdKey, string.Empty) ?? string.Empty;
            set => options.SetValueString(SelectedSiteIdKey, value ?? string.Empty);
        }

        public ObservingSite SelectedSite {
            get {
                if (!Guid.TryParse(SelectedSiteId, out var id)) {
                    return null;
                }
                return SiteCatalog.Shared.Find(id);
            }
        }

        /// <summary>
        /// Updates the active profile's coordinates. NINA's profile service raises
        /// LocationChanged and schedules the profile save, just like editing the
        /// latitude and longitude fields in Options &gt; General.
        /// </summary>
        public void Apply(ObservingSite site) {
            if (site == null || profiles.ActiveProfile == null) {
                return;
            }

            profiles.ChangeLatitude(site.Latitude);
            profiles.ChangeLongitude(site.Longitude);
            profiles.ChangeElevation(site.Elevation);
            SelectedSiteId = site.Id.ToString();
            OnSelectionApplied();
        }

        /// <summary>
        /// Creates a new unsaved site from the active profile's current coordinates.
        /// </summary>
        public ObservingSite CreateSiteFromActiveProfile() {
            var astrometry = profiles.ActiveProfile?.AstrometrySettings;
            if (astrometry == null) {
                return null;
            }

            return new ObservingSite {
                Name = "Current location",
                Latitude = astrometry.Latitude,
                Longitude = astrometry.Longitude,
                Elevation = astrometry.Elevation
            };
        }

        public void Dispose() {
            if (!initialized) {
                return;
            }

            profiles.ProfileChanged -= OnProfileChanged;
            profiles.LocationChanged -= OnLocationChanged;
            initialized = false;
        }

        private void OnProfileChanged(object sender, EventArgs e) {
            OnSelectionApplied();
        }

        private void OnLocationChanged(object sender, EventArgs e) {
            OnSelectionApplied();
        }

        private static void OnSelectionApplied() {
            var handler = SelectionApplied;
            handler?.Invoke();
        }
    }
}
