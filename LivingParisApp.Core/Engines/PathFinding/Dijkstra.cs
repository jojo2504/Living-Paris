using LivingParisApp.Core.Entities.Station;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;
using LivingParisApp.Services.Logging;

namespace LivingParisApp.Core.Engines.ShortestPaths {
    public class Dijkstra<T> : ShortestPathsEngine<T> where T : IStation<T> {

        ///<summary>
        /// Initializes the Dijkstra algorithm by computing the shortest paths from a starting node to all other nodes in the graph.
        /// </summary>
        /// <param name="map">The graph represented as a Map<T>, containing nodes and their adjacency lists with weights.</param>
        /// <param name="start">The starting node from which to compute the shortest paths.</param>
        public void Init(Map<T> map, Node<T> start) {
            if (start != null) {
                // Find the correct node instance in the map
                _startVertice = map.AdjacencyList.Keys.FirstOrDefault(n => n.Object.ID == start.Object.ID);
                if (_startVertice == null) _startVertice = start; // Fallback

                _distances = new Dictionary<Node<T>, double>();
                _predecessors = new Dictionary<Node<T>, Node<T>>();

                PriorityQueue<Node<T>, double> priorityQueue = new PriorityQueue<Node<T>, double>();

                foreach (var node in map.AdjacencyList.Keys) {
                    if (node.Object.ID == _startVertice.Object.ID) {
                        _distances[node] = 0;
                        priorityQueue.Enqueue(node, 0);
                    }
                    else {
                        _distances[node] = double.PositiveInfinity;
                        priorityQueue.Enqueue(node, double.PositiveInfinity);
                    }
                    _predecessors[node] = null;
                }

                // Rest of your implementation stays the same
            }
        }

        /// <summary>
        /// Retrieves the shortest path from the starting node to a target node, along with the total length of the path.
        /// </summary>
        /// <param name="to">The target node to which the shortest path is computed.</param>
        /// <returns>
        /// A tuple containing:
        /// - A LinkedList<Node<T>> representing the shortest path from the start node to the target node.
        /// - A double representing the total length of the path.
        /// If no path exists, returns an empty LinkedList and a total length of double.PositiveInfinity.
        /// </returns>
        public (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> to) {
            // Find the correct node instance in our data structures
            var targetNode = _distances.Keys.FirstOrDefault(n => n.Object.ID == to.Object.ID);
            if (targetNode == null || _distances[targetNode] == double.PositiveInfinity) {
                return (new LinkedList<Node<T>>(), double.PositiveInfinity);
            }

            double totalLength = _distances[targetNode];
            List<Node<T>> pathList = new List<Node<T>>();
            pathList.Add(targetNode);

            Node<T> current = targetNode;
            while (current.Object.ID != _startVertice.Object.ID) {
                if (_predecessors[current] == null) {
                    return (new LinkedList<Node<T>>(), double.PositiveInfinity);
                }
                current = _predecessors[current];
                pathList.Insert(0, current);
            }

            LinkedList<Node<T>> path = new LinkedList<Node<T>>(pathList);
            return (path, totalLength);
        }
    }
}