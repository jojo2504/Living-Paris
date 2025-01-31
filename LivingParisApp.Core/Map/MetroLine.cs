namespace LivingParisApp.Core.Map {
    internal class MetroLine {
        public int LineId { get; }
        public string LineName { get; }
        public HashSet<MetroStation> Stations { get; }

        public MetroLine() {
            Stations = new();
        }

        public void AddStationInLine(MetroStation metroStation) {
            if (metroStation == null) {
                throw new ArgumentNullException("");
            }
            Stations.Add(metroStation);
        }

    }
}
