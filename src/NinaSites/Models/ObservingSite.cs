using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace CraigDavey.Nina.Sites.Models {
    /// <summary>
    /// A saved observing location. Coordinates use NINA's convention: negative
    /// longitude is west of Greenwich and elevation is in metres.
    /// </summary>
    public sealed class ObservingSite {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Elevation { get; set; }

        public string Notes { get; set; } = string.Empty;

        [JsonIgnore]
        public string Coordinates => string.Format(
            CultureInfo.InvariantCulture,
            "{0:F5}, {1:F5}",
            Latitude,
            Longitude);

        public ObservingSite Clone() {
            return new ObservingSite {
                Id = Id,
                Name = Name,
                Latitude = Latitude,
                Longitude = Longitude,
                Elevation = Elevation,
                Notes = Notes
            };
        }

        public bool IsValid(out string error) {
            if (string.IsNullOrWhiteSpace(Name)) {
                error = "Enter a name for the site.";
                return false;
            }

            if (!double.IsFinite(Latitude) || Latitude < -90 || Latitude > 90) {
                error = "Latitude must be between -90 and 90.";
                return false;
            }

            if (!double.IsFinite(Longitude) || Longitude < -180 || Longitude > 180) {
                error = "Longitude must be between -180 and 180.";
                return false;
            }

            if (!double.IsFinite(Elevation)) {
                error = "Elevation must be a number.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
