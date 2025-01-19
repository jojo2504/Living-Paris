namespace LivingParisApp.Core.Map {
    internal class MetroStation {
        public string Name { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public bool IsTerminus { get; }
        public int WaitingTime { get; }
        public HashSet<string> Connections { get; } // Lines that connect at this station

        const double earthRadius = 6371; // Earth's radius in kilometers

        public MetroStation(string name, double latitude, double longitude, bool isTerminus = false) {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            IsTerminus = isTerminus;
            Connections = new HashSet<string>();
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