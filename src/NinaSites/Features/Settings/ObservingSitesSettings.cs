using CraigDavey.Nina.Sites.Models;
using CraigDavey.Nina.Sites.Services;
using NINA.Core.Utility;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CraigDavey.Nina.Sites.Features.Settings {
    /// <summary>
    /// Data context for the plugin's Settings tab under Options &gt; Plugins.
    /// The options template receives the manifest as DataContext and binds to
    /// <c>Settings.*</c>.
    /// </summary>
    [Export(typeof(ObservingSitesSettings))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ObservingSitesSettings : BaseINPC, IDisposable {
        private readonly SiteProfileService profileSites;
        private readonly ObservableCollection<ObservingSite> sites = new ObservableCollection<ObservingSite>();
        private ObservingSite selectedSite;
        private ObservingSite editingSite;
        private string statusMessage = string.Empty;
        private bool initialized;

        [ImportingConstructor]
        public ObservingSitesSettings(SiteProfileService profileSites) {
            this.profileSites = profileSites;
            NewSiteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(StartNewSite);
            UseCurrentLocationCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(AddCurrentLocation);
            SaveSiteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(SaveSite);
            DeleteSiteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(DeleteSite);
            ApplySiteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ApplySite);
        }

        public ObservableCollection<ObservingSite> Sites => sites;

        public ICommand NewSiteCommand { get; }

        public ICommand UseCurrentLocationCommand { get; }

        public ICommand SaveSiteCommand { get; }

        public ICommand DeleteSiteCommand { get; }

        public ICommand ApplySiteCommand { get; }

        public ObservingSite SelectedSite {
            get => selectedSite;
            set {
                if (ReferenceEquals(selectedSite, value)) {
                    return;
                }
                selectedSite = value;
                RaisePropertyChanged();
                EditingSite = value?.Clone() ?? new ObservingSite();
            }
        }

        /// <summary>
        /// Editable copy of the selected site. Changes are only written to the
        /// shared catalog when Save Site is clicked.
        /// </summary>
        public ObservingSite EditingSite {
            get => editingSite;
            private set {
                editingSite = value;
                RaisePropertyChanged();
            }
        }

        public string StatusMessage {
            get => statusMessage;
            private set {
                statusMessage = value ?? string.Empty;
                RaisePropertyChanged();
            }
        }

        public string ActiveProfileName => profileSites.ProfileName;

        public string CurrentLocation => profileSites.CurrentLocation;

        public void Initialize() {
            if (initialized) {
                return;
            }

            profileSites.Initialize();
            SiteCatalog.Shared.Changed += OnCatalogChanged;
            SiteProfileService.SelectionApplied += OnSelectionApplied;
            ReloadSites();
            SelectSite(profileSites.SelectedSite);
            initialized = true;
            RaisePropertyChanged(nameof(ActiveProfileName));
            RaisePropertyChanged(nameof(CurrentLocation));
        }

        public void Dispose() {
            if (!initialized) {
                return;
            }

            SiteCatalog.Shared.Changed -= OnCatalogChanged;
            SiteProfileService.SelectionApplied -= OnSelectionApplied;
            initialized = false;
        }

        private void StartNewSite() {
            SelectedSite = null;
            EditingSite = new ObservingSite { Name = "New site" };
            StatusMessage = "Enter the site details, then click Save Site.";
        }

        private void AddCurrentLocation() {
            var site = profileSites.CreateSiteFromActiveProfile();
            if (site == null) {
                StatusMessage = "There is no active profile to read coordinates from.";
                return;
            }

            SiteCatalog.Shared.Save(site);
            SelectSite(site);
            StatusMessage = $"Saved the current profile location as '{site.Name}'.";
        }

        private void SaveSite() {
            if (editingSite == null) {
                return;
            }

            if (!editingSite.IsValid(out var error)) {
                StatusMessage = error;
                MessageBox.Show(error, "Site Switcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saved = editingSite.Clone();
            SiteCatalog.Shared.Save(saved);
            SelectSite(saved);
            StatusMessage = $"Saved '{saved.Name}'.";
        }

        private void DeleteSite() {
            if (selectedSite == null) {
                StatusMessage = "Select a site to delete.";
                return;
            }

            var name = selectedSite.Name;
            var result = MessageBox.Show(
                $"Delete '{name}'?",
                "Site Switcher",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) {
                return;
            }

            SiteCatalog.Shared.Delete(selectedSite.Id);
            SelectedSite = null;
            StatusMessage = $"Deleted '{name}'.";
        }

        private void ApplySite() {
            if (selectedSite == null) {
                StatusMessage = "Select a site to apply.";
                return;
            }

            profileSites.Apply(selectedSite);
            StatusMessage = $"Applied '{selectedSite.Name}' to profile '{profileSites.ProfileName}'.";
        }

        private void OnCatalogChanged(object sender, EventArgs e) {
            var previousId = selectedSite?.Id;
            ReloadSites();
            if (previousId.HasValue) {
                SelectById(previousId.Value);
            }
        }

        private void OnSelectionApplied() {
            SelectSite(profileSites.SelectedSite);
            RaisePropertyChanged(nameof(ActiveProfileName));
            RaisePropertyChanged(nameof(CurrentLocation));
        }

        private void ReloadSites() {
            sites.Clear();
            foreach (var site in SiteCatalog.Shared.Sites) {
                sites.Add(site);
            }
        }

        private void SelectSite(ObservingSite site) {
            if (site == null) {
                SelectedSite = null;
                return;
            }
            SelectById(site.Id);
        }

        private void SelectById(Guid id) {
            SelectedSite = sites.FirstOrDefault(s => s.Id == id);
        }
    }
}
