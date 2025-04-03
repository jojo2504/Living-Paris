using LivingParisApp.Core.Mapping;
using LivingParisApp.Core.GraphStructure;

namespace LivingParisApp.Core.Engines.ShortestPaths {
    public class ShortestPathsEngine<T> where T : IStation<T> {
        public double[,]? distances {get; set;}
        public int[,]? predecessors {get; set;}
        public Dictionary<Node<T>,double>? _distances {get;set;}
        public Dictionary<Node<T>,Node<T>>? _predecessors {get;set;}
        public Node<T>? _startVertice {get;set;}

    }
}