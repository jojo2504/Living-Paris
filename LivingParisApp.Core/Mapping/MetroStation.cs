using System.Globalization;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Services.Logging;

namespace LivingParisApp.Core.Mapping {
    public class MetroStation : IStation<MetroStation>{
        public int ID { get; }
        public string LibelleLine { get; }
        public string LibelleStation { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        public string CommuneName { get; }
        public string CommuneCode { get; }
        const double earthRadius = 6371; // Earth's radius in kilometers

        public MetroStation(int ID, string libelleLine, string libelleStation, double longitude, double latitude, string communeName, string communeCode) {
            this.ID = ID;
            LibelleLine = libelleLine;
            LibelleStation = libelleStation;
            Longitude = longitude;
            Latitude = latitude;
            CommuneName = communeName;
            CommuneCode = communeCode;
        }

        public static MetroStation FromParts(string[] parts) {
            return new MetroStation(
                Convert.ToInt32(parts[0]),
                parts[1],
                parts[2],
                string.IsNullOrEmpty(parts[3]) ? 0 : Convert.ToDouble(parts[3], CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(parts[4]) ? 0 : Convert.ToDouble(parts[4], CultureInfo.InvariantCulture),
                parts[5],
                parts[6]
            );
        }

        public double CalculateDistance(MetroStation other) {
            var lat1 = ConvertToRadians(Latitude);
            var lat2 = ConvertToRadians(other.Latitude);
            var lon1 = ConvertToRadians(Longitude);
            var lon2 = ConvertToRadians(other.Longitude);
            var deltaLat = lat2 - lat1;
            var deltaLon = lon2 - lon1; 

            var a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLon / 2), 2);
            var distance = earthRadius * 2 * Math.Asin(Math.Sqrt(a));

            return distance;
        }

        private double ConvertToRadians(double angle) {
            return Math.PI / 180 * angle;
        }
    }
}