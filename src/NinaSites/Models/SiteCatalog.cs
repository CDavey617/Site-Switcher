using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CraigDavey.Nina.Sites.Models {
    /// <summary>
    /// Persists the shared site list under NINA's plugin settings folder.
    /// A single instance is used by the options page and the dock panel even
    /// though NINA composes those parts in different MEF containers.
    /// </summary>
    public sealed class SiteCatalog {
        private const int FileVersion = 1;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };
        private static readonly Lazy<SiteCatalog> SharedInstance = new Lazy<SiteCatalog>(() => new SiteCatalog(DefaultFilePath()));

        private readonly object gate = new object();
        private readonly string filePath;
        private List<ObservingSite> sites = new List<ObservingSite>();

        public SiteCatalog(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) {
                throw new ArgumentException("A file path is required.", nameof(filePath));
            }

            this.filePath = filePath;
            Load();
        }

        public static SiteCatalog Shared => SharedInstance.Value;

        public event EventHandler Changed;

        public IReadOnlyList<ObservingSite> Sites {
            get {
                lock (gate) {
                    return sites
                        .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();
                }
            }
        }

        public ObservingSite Find(Guid id) {
            lock (gate) {
                return sites.FirstOrDefault(s => s.Id == id)?.Clone();
            }
        }

        public void Save(ObservingSite site) {
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }

            if (!site.IsValid(out var error)) {
                throw new ArgumentException(error, nameof(site));
            }

            var copy = site.Clone();
            if (copy.Id == Guid.Empty) {
                copy.Id = Guid.NewGuid();
            }

            lock (gate) {
                var index = sites.FindIndex(s => s.Id == copy.Id);
                if (index >= 0) {
                    sites[index] = copy;
                } else {
                    sites.Add(copy);
                }

                Persist();
            }

            OnChanged();
        }

        public void Delete(Guid id) {
            bool removed;
            lock (gate) {
                removed = sites.RemoveAll(s => s.Id == id) > 0;
                if (removed) {
                    Persist();
                }
            }

            if (removed) {
                OnChanged();
            }
        }

        private static string DefaultFilePath() {
            var pluginId = typeof(Plugin).Assembly.GetCustomAttribute<GuidAttribute>()?.Value ?? "ObservingSites";
            return Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginSettings", pluginId, "sites.json");
        }

        private void Load() {
            if (!File.Exists(filePath)) {
                return;
            }

            try {
                var document = JsonSerializer.Deserialize<SiteCatalogFile>(File.ReadAllText(filePath), JsonOptions);
                if (document?.Sites == null) {
                    return;
                }

                sites = document.Sites
                    .Where(s => s != null && s.IsValid(out _))
                    .Select(s => {
                        if (s.Id == Guid.Empty) {
                            s.Id = Guid.NewGuid();
                        }
                        return s;
                    })
                    .ToList();
            } catch (Exception ex) {
                Logger.Error($"Site Switcher: could not read {filePath}. Starting with an empty site list.", ex);
                sites = new List<ObservingSite>();
            }
        }

        private void Persist() {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) {
                Directory.CreateDirectory(directory);
            }

            var document = new SiteCatalogFile { Version = FileVersion, Sites = sites };
            var temporaryPath = filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, JsonOptions));
            File.Move(temporaryPath, filePath, true);
        }

        private void OnChanged() {
            var handler = Changed;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public sealed class SiteCatalogFile {
            public int Version { get; set; }

            public List<ObservingSite> Sites { get; set; } = new List<ObservingSite>();
        }
    }
}
