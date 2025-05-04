using LivingParisApp.Core.GraphStructure;

namespace LivingParisApp.Core.Entities.Station {
    public class MetroStationLink<T> : Link<T> where T : IStation<T> {
        public string Line {get; set;}
        public MetroStationLink(Node<T> A, Node<T> B, Direction direction, double weight = 1, string line = "") : base(A, B, direction, weight) {
            Line = line;
        }
    }
}