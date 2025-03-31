using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public class Dijkstra<T> : ShortestPathsEngine<T> where T : IStation {
        public override void Init(Map<T> map, Node<T>? first = default) {
            throw new NotImplementedException();
        }

        public override (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> from, Node<T> to) {
            throw new NotImplementedException();
        }
    }
}