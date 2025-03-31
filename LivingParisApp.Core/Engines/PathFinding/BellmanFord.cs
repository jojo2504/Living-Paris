using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public class BellmanFord<T> : ShortestPathsEngine<T> where T : IStation {

        private Node<T> _startVertice; 

        /// <summary>
        /// Initializes the algorithm with a map and a starting vertex.
        /// </summary>
        /// <param name="map">The map containing nodes and their connections</param>
        /// <param name="start">The starting node for path calculations</param>
        public void Init(Map<T> map, Node<T> start) {

            if(start != null){
                _startVertice = start;
                _distances = new Dictionary<Node<T>,double>();
                _predecessors = new Dictionary<Node<T>, Node<T>>();

                foreach (var node in map.AdjacencyList){
                    if(node.Key == _startVertice){
                        _distances[node.Key] = 0;
                    }
                    else{
                        _distances[node.Key] = double.PositiveInfinity;
                        _predecessors[node.Key] = _startVertice;
                    }
                }

                int iteration = 1;
                bool noChange = false;
                while(iteration <= map.AdjacencyList.Keys.Count() - 1 && !noChange){
                    noChange = true;
                    foreach (var node in map.AdjacencyList){
                        foreach (var neighbor in node.Value){
                            if(_distances[node.Key] != double.PositiveInfinity && 
                            neighbor.Item1 != _startVertice && 
                            _distances[neighbor.Item1] > _distances[node.Key]+neighbor.Item2){
                                _distances[neighbor.Item1] = _distances[node.Key]+neighbor.Item2;
                                _predecessors[neighbor.Item1] = node.Key;
                                noChange = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the shortest path to a specified node and its total length.
        /// </summary>
        /// <param name="to">The destination node</param>
        /// <returns>A tuple containing the path (LinkedList) and total length</returns>
        public (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> to) {

            if (_distances[to] == double.PositiveInfinity) {
                return (new LinkedList<Node<T>>(), double.PositiveInfinity);
            }

            List<Node<T>> pathList = new List<Node<T>>();
            pathList.Add(to); 

            while (_startVertice != to){
                to = _predecessors[to];
                pathList.Insert(0, to); 
            }
            LinkedList<Node<T>> path = new LinkedList<Node<T>>(pathList);

            return (path,_distances[to]);
        }
    }
}