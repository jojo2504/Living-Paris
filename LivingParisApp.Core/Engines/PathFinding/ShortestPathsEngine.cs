using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public abstract class ShortestPathsEngine<T> {
        private readonly double[,]? distances;
        private readonly int[,]? predecessors;
        public abstract void Init(Map map);
        public abstract (LinkedList<T> Path, double TotalLength) GetPath(T from, T to);
    }
}