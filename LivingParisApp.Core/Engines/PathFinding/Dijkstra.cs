using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public class Dijkstra<T> : ShortestPathsEngine<T> where T : IStation<T> {
        public void Init(Map<T> map, Node<T> from) {
            throw new NotImplementedException();
        }
    
        public (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> to) {
            throw new NotImplementedException();
        }
    }
}