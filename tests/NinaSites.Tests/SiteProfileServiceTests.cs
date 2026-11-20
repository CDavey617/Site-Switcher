using CraigDavey.Nina.Sites.Models;
using CraigDavey.Nina.Sites.Services;
using Moq;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;

namespace CraigDavey.Nina.Sites.Tests {
    public class SiteProfileServiceTests {
        [Test]
        public void ApplyUpdatesActiveProfileCoordinates() {
            var astrometry = new Mock<IAstrometrySettings>();
            astrometry.SetupProperty(a => a.Latitude);
            astrometry.SetupProperty(a => a.Longitude);
            astrometry.SetupProperty(a => a.Elevation);

            var profile = new Mock<IProfile>();
            profile.SetupGet(p => p.PluginSettings).Returns(new NINA.Profile.PluginSettings());
            profile.SetupGet(p => p.AstrometrySettings).Returns(astrometry.Object);

            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(p => p.ActiveProfile).Returns(profile.Object);

            var site = new ObservingSite {
                Name = "Test site",
                Latitude = 40.7128,
                Longitude = -74.0060,
                Elevation = 12
            };

            using var service = new SiteProfileService(profiles.Object);
            service.Apply(site);

            Assert.Multiple(() => {
                Assert.That(astrometry.Object.Latitude, Is.EqualTo(site.Latitude));
                Assert.That(astrometry.Object.Longitude, Is.EqualTo(site.Longitude));
                Assert.That(astrometry.Object.Elevation, Is.EqualTo(site.Elevation));
                Assert.That(service.SelectedSiteId, Is.EqualTo(site.Id.ToString()));
            });
            profiles.Verify(p => p.ChangeLatitude(site.Latitude), Times.Once);
            profiles.Verify(p => p.ChangeLongitude(site.Longitude), Times.Once);
            profiles.Verify(p => p.ChangeElevation(site.Elevation), Times.Once);
        }

        [Test]
        public void CreateSiteFromActiveProfileCopiesCurrentCoordinates() {
            var astrometry = new Mock<IAstrometrySettings>();
            astrometry.SetupGet(a => a.Latitude).Returns(12.5);
            astrometry.SetupGet(a => a.Longitude).Returns(-42.25);
            astrometry.SetupGet(a => a.Elevation).Returns(500);

            var profile = new Mock<IProfile>();
            profile.SetupGet(p => p.PluginSettings).Returns(new NINA.Profile.PluginSettings());
            profile.SetupGet(p => p.AstrometrySettings).Returns(astrometry.Object);

            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(p => p.ActiveProfile).Returns(profile.Object);

            using var service = new SiteProfileService(profiles.Object);
            var site = service.CreateSiteFromActiveProfile();

            Assert.That(site, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(site.Latitude, Is.EqualTo(12.5));
                Assert.That(site.Longitude, Is.EqualTo(-42.25));
                Assert.That(site.Elevation, Is.EqualTo(500));
                Assert.That(site.Name, Is.Not.Empty);
            });
        }
    }
}
