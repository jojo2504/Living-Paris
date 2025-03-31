using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths {
    public abstract class ShortestPathsEngine<T> where T : IStation {
        public double[,]? distances {get; set;}
        public int[,]? predecessors {get; set;}
    }
}