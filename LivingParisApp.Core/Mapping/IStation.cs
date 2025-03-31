namespace LivingParisApp.Core.Mapping {
    public interface IStation<T> where T : IStation<T> {
        public int ID { get; }
        public string LibelleLine { get; }
        public string LibelleStation { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        public string CommuneName { get; }
        public string CommuneCode { get; }

        static abstract T FromParts(string[] parts);
    }
}