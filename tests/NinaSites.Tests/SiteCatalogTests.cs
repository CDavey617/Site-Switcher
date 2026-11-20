using CraigDavey.Nina.Sites.Models;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace CraigDavey.Nina.Sites.Tests {
    public class SiteCatalogTests {
        private static string NewPath() {
            return Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "catalogs",
                Guid.NewGuid().ToString("N"),
                "sites.json");
        }

        [Test]
        public void SavesUpdatesAndDeletesSites() {
            var path = NewPath();
            var catalog = new SiteCatalog(path);
            var site = new ObservingSite {
                Name = "Home",
                Latitude = 51.5,
                Longitude = -0.12,
                Elevation = 85,
                Notes = "Back garden"
            };

            catalog.Save(site);
            Assert.That(catalog.Sites.Single().Name, Is.EqualTo("Home"));

            site.Latitude = 52.25;
            catalog.Save(site);
            Assert.That(catalog.Sites.Single().Latitude, Is.EqualTo(52.25));

            catalog.Delete(site.Id);
            Assert.That(catalog.Sites, Is.Empty);
        }

        [Test]
        public void ReloadsSitesFromDisk() {
            var path = NewPath();
            var catalog = new SiteCatalog(path);
            catalog.Save(new ObservingSite { Name = "Dark sky", Latitude = -33.9, Longitude = 151.2, Elevation = 1200 });

            var reloaded = new SiteCatalog(path);
            var site = reloaded.Sites.Single();
            Assert.Multiple(() => {
                Assert.That(site.Name, Is.EqualTo("Dark sky"));
                Assert.That(site.Latitude, Is.EqualTo(-33.9));
                Assert.That(site.Longitude, Is.EqualTo(151.2));
                Assert.That(site.Elevation, Is.EqualTo(1200));
            });
        }

        [Test]
        public void RejectsInvalidCoordinates() {
            var catalog = new SiteCatalog(NewPath());
            Assert.That(
                () => catalog.Save(new ObservingSite { Name = "Bad", Latitude = 91, Longitude = 0 }),
                Throws.ArgumentException);
            Assert.That(
                () => catalog.Save(new ObservingSite { Name = "Bad", Latitude = 0, Longitude = -181 }),
                Throws.ArgumentException);
            Assert.That(
                () => catalog.Save(new ObservingSite { Name = string.Empty, Latitude = 0, Longitude = 0 }),
                Throws.ArgumentException);
        }

        [Test]
        public void RaisesChangedWhenCatalogIsModified() {
            var catalog = new SiteCatalog(NewPath());
            var changes = 0;
            catalog.Changed += (sender, args) => changes++;
            var site = new ObservingSite { Name = "One", Latitude = 1, Longitude = 2 };

            catalog.Save(site);
            catalog.Save(site.Clone());
            catalog.Delete(site.Id);

            Assert.That(changes, Is.EqualTo(3));
        }
    }
}
