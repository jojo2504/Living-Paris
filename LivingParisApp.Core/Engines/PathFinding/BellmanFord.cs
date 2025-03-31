using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public class BellmanFord<T> : ShortestPathsEngine<T> where T : IStation {
        public void Init(Map<T> map, Node<T> from) {
            throw new NotImplementedException();
        }

        public (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> to) {
            throw new NotImplementedException();
        }
    }
}