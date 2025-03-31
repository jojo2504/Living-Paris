using LivingParisApp.Core.GraphStructure;

namespace LivingParisApp.Core.Mapping {
    public class MetroStation : IStation {
        public int ID { get; }
        public string LibelleLine { get; }
        public string LibelleStation { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        public string CommuneName { get; }
        public string CommuneCode { get; }
        const double earthRadius = 6371; // Earth's radius in kilometers
        private string v;

        public MetroStation(int ID, string LibelleLine, string libelleStation, double latitude, double longitude, string CommuneName, string CommuneCode) {
            this.ID = ID;
            LibelleLine = LibelleLine;
            LibelleStation = libelleStation;
            Latitude = latitude;
            Longitude = longitude;
            CommuneName = CommuneName;
            CommuneCode = CommuneCode;
        }

        public MetroStation(string v) {
            this.v = v;
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