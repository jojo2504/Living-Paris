using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public class Dijkstra<T> : ShortestPathsEngine<T> where T : IStation {
        public void Init(Map<T> map, Node<T> from) {
            var a = 1;
            throw new NotImplementedException();
        }
    
        public (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> to) {
            throw new NotImplementedException();
        }
    }
}