using CraigDavey.Nina.Sites.Features.Dock;
using Moq;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Windows;

namespace CraigDavey.Nina.Sites.Tests {
    [NonParallelizable, Apartment(ApartmentState.STA)]
    public class CompositionTests {
        private static CompositionContainer Create(Mock<IProfileService> profiles) {
            var container = new CompositionContainer(new AssemblyCatalog(typeof(Plugin).Assembly));
            container.ComposeExportedValue(profiles.Object);
            return container;
        }

        private static Mock<IProfileService> CreateProfiles() {
            var profile = new Mock<IProfile>();
            profile.SetupGet(p => p.PluginSettings).Returns(new NINA.Profile.PluginSettings());

            var astrometry = new Mock<IAstrometrySettings>();
            astrometry.SetupGet(a => a.Latitude).Returns(0);
            astrometry.SetupGet(a => a.Longitude).Returns(0);
            astrometry.SetupGet(a => a.Elevation).Returns(0);
            profile.SetupGet(p => p.AstrometrySettings).Returns(astrometry.Object);

            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(p => p.ActiveProfile).Returns(profile.Object);
            return profiles;
        }

        [Test]
        public async Task ComposesManifestDockPanelAndSettings() {
            var profiles = CreateProfiles();
            using var manifestContainer = Create(profiles);
            using var partsContainer = Create(profiles);

            var manifest = manifestContainer.GetExportedValue<IPluginManifest>();
            var panels = partsContainer.GetExportedValues<IDockableVM>().ToList();

            await manifest.Initialize();
            Assert.That(panels.Single(), Is.TypeOf<SitesPanel>());

            await manifest.Teardown();
        }

        [Test]
        public void ExportedResourceDictionariesAndTemplatesConstruct() {
            if (Application.Current == null) {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }

            foreach (var type in typeof(Plugin).Assembly.GetTypes().Where(t => typeof(ResourceDictionary).IsAssignableFrom(t))) {
                var dictionary = (ResourceDictionary)Activator.CreateInstance(type);
                foreach (var template in dictionary.Values.OfType<DataTemplate>()) {
                    Assert.That(template.LoadContent(), Is.Not.Null, type.FullName);
                }
            }
        }
    }
}
