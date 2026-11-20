using CraigDavey.Nina.Sites.Models;
using CraigDavey.Nina.Sites.Services;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace CraigDavey.Nina.Sites.Features.Dock {
    /// <summary>
    /// Small dockable panel for switching sites without leaving the imaging tab.
    /// </summary>
    [Export(typeof(IDockableVM))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class SitesPanel : BaseINPC, IDockableVM, IDisposable {
        private readonly SiteProfileService profileSites;
        private readonly ObservableCollection<ObservingSite> sites = new ObservableCollection<ObservingSite>();
        private ObservingSite selectedSite;
        private string statusMessage = string.Empty;
        private bool initialized;
        private bool visible = true;

        [ImportingConstructor]
        public SitesPanel(SiteProfileService profileSites) {
            this.profileSites = profileSites;
            ApplyCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ApplySelectedSite);
            HideCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<object>(Hide);
            ImageGeometry = CreateImageGeometry();

            profileSites.Initialize();
            SiteCatalog.Shared.Changed += OnCatalogChanged;
            SiteProfileService.SelectionApplied += OnSelectionApplied;
            Plugin.ShuttingDown += Dispose;

            ReloadSites();
            SelectSite(profileSites.SelectedSite);
            initialized = true;
        }

        public ObservableCollection<ObservingSite> Sites => sites;

        public ICommand ApplyCommand { get; }

        public ICommand HideCommand { get; }

        public ObservingSite SelectedSite {
            get => selectedSite;
            set {
                if (ReferenceEquals(selectedSite, value)) {
                    return;
                }
                selectedSite = value;
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

        public string ContentId => typeof(SitesPanel).FullName;

        public string Title { get; set; } = "Site Switcher";

        public bool CanClose { get; set; } = true;

        public bool IsClosed { get; set; }

        public bool HasSettings { get; set; }

        public bool IsTool => false;

        public bool IsVisible {
            get => visible;
            set {
                visible = value;
                RaisePropertyChanged();
            }
        }

        public GeometryGroup ImageGeometry { get; set; }

        public void Hide(object value) {
            IsVisible = !IsVisible;
        }

        public void Dispose() {
            if (!initialized) {
                return;
            }

            initialized = false;
            SiteCatalog.Shared.Changed -= OnCatalogChanged;
            SiteProfileService.SelectionApplied -= OnSelectionApplied;
            Plugin.ShuttingDown -= Dispose;
        }

        private void ApplySelectedSite() {
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

        private static GeometryGroup CreateImageGeometry() {
            var group = new GeometryGroup();
            group.Children.Add(Geometry.Parse("M0,0 L10,0 10,10 0,10 Z"));
            group.Freeze();
            return group;
        }
    }
}
