namespace LivingParisApp.Core.Entities.Station {
    public class MetroStation : IStation<MetroStation>{
        public int ID { get; }
        public string LibelleLine { get; }
        public string LibelleStation { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        public string CommuneName { get; }
        public string CommuneCode { get; }
        const double earthRadius = 6371; // Earth's radius in kilometers

        public MetroStation(int ID, string LibelleLine, string libelleStation, double latitude, double longitude, string CommuneName, string CommuneCode) {
            this.ID = ID;
            LibelleLine = LibelleLine;
            LibelleStation = libelleStation;
            Latitude = latitude;
            Longitude = longitude;
            CommuneName = CommuneName;
            CommuneCode = CommuneCode;
        }

        public static MetroStation FromParts(string[] parts) {
            return new MetroStation(
                Convert.ToInt32(parts[0]),
                parts[1],
                parts[2],
                string.IsNullOrEmpty(parts[3]) ? 0 : Convert.ToDouble(parts[3]),
                string.IsNullOrEmpty(parts[4]) ? 0 : Convert.ToDouble(parts[4]),
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