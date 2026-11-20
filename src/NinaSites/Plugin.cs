using NINA.Plugin;
using NINA.Plugin.Interfaces;
using System.ComponentModel.Composition;

namespace CraigDavey.Nina.Sites {
    /// <summary>
    /// Manifest and lifecycle for the Site Switcher plugin.
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class Plugin : PluginBase {
        /// <summary>
        /// MEF composes the manifest and dock panels in separate containers. This
        /// assembly-scoped event lets the manifest reach the displayed panel instance.
        /// </summary>
        internal static event Action ShuttingDown;

        [Import]
        public Features.Settings.ObservingSitesSettings Settings { get; set; }

        [Import]
        public Services.SiteProfileService ProfileSites { get; set; }

        public override Task Initialize() {
            ProfileSites?.Initialize();
            Settings?.Initialize();
            return Task.CompletedTask;
        }

        public override Task Teardown() {
            ShuttingDown?.Invoke();
            Settings?.Dispose();
            ProfileSites?.Dispose();
            return Task.CompletedTask;
        }
    }
}
