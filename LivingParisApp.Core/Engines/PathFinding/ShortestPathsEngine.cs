using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public abstract class ShortestPathsEngine<T> where T : IStation{
        public double[,]? distances;
        public int[,]? predecessors;
        public abstract void Init(Map<T> map, T? first = default);
        public abstract (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> from, Node<T> to);
    }
}