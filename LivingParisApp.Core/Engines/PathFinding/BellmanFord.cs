using LivingParisApp.Core.Entities.Station;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;
using LivingParisApp.Services.Logging;

namespace LivingParisApp.Core.Engines.ShortestPaths {
    public class BellmanFord<T> : ShortestPathsEngine<T> where T : IStation<T> {

        /// <summary>
        /// Initializes the algorithm with a map and a starting vertex.
        /// </summary>
        /// <param name="map">The map containing nodes and their connections</param>
        /// <param name="start">The starting node for path calculations</param>
        public int Init(Map<T> map, Node<T> start) {
            if (start != null) {
                _startVertice = start;
                _distances = new Dictionary<Node<T>, double>();
                _predecessors = new Dictionary<Node<T>, Node<T>>();

                foreach (var node in map.AdjacencyList) {
                    if (node.Key.Object.ID == _startVertice.Object.ID) {
                        _distances[node.Key] = 0;
                    }
                    else {
                        _distances[node.Key] = double.PositiveInfinity;
                    }
                    _predecessors[node.Key] = null;
                }

                // Relax all edges |V| - 1 times
                int vertexCount = map.AdjacencyList.Keys.Count();
                for (int i = 1; i <= vertexCount - 1; i++) {
                    foreach (var node in map.AdjacencyList) {
                        foreach (var neighbor in node.Value) {
                            if (_distances[node.Key] != double.PositiveInfinity &&
                                _distances[neighbor.Item1] > _distances[node.Key] + neighbor.Item2) {
                                _distances[neighbor.Item1] = _distances[node.Key] + neighbor.Item2;
                                _predecessors[neighbor.Item1] = node.Key;
                            }
                        }
                    }
                }

                // Check for negative-weight cycles
                foreach (var node in map.AdjacencyList) {
                    foreach (var neighbor in node.Value) {
                        if (_distances[node.Key] != double.PositiveInfinity &&
                            _distances[neighbor.Item1] > _distances[node.Key] + neighbor.Item2) {
                            Logger.Warning("There is a negative-weight cycles.");
                            return 1; // indicate no success
                        }
                    }
                }
            }

            return 0; // indicate success
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

            double totalLength = _distances[to];
            List<Node<T>> pathList = new List<Node<T>>();
            pathList.Add(to);

            while (_startVertice.Object.ID != to.Object.ID) {
                if (_predecessors[to] == null) {
                    return (new LinkedList<Node<T>>(), double.PositiveInfinity);
                }
                to = _predecessors[to];
                pathList.Insert(0, to);
            }
            LinkedList<Node<T>> path = new LinkedList<Node<T>>(pathList);

            return (path, totalLength);
        }
    }
}