using LivingParisApp.Core.Entities.Station;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public class Dijkstra<T> : ShortestPathsEngine<T> where T : IStation<T> {

        ///<summary>
        /// Initializes the Dijkstra algorithm by computing the shortest paths from a starting node to all other nodes in the graph.
        /// </summary>
        /// <param name="map">The graph represented as a Map<T>, containing nodes and their adjacency lists with weights.</param>
        /// <param name="start">The starting node from which to compute the shortest paths.</param>
        public void Init(Map<T> map, Node<T> start) {
            if(start!=null){
                _startVertice = start;
                _distances = new Dictionary<Node<T>, double>();
                _predecessors = new Dictionary<Node<T>, Node<T>>();

                PriorityQueue<Node<T>,double> priorityQueue = new PriorityQueue<Node<T>, double>();

                foreach (var node in map.AdjacencyList) {
                    if (node.Key.Object.ID == _startVertice.Object.ID) {
                        _distances[node.Key] = 0;
                        priorityQueue.Enqueue(node.Key,0);
                    }
                    else {
                        _distances[node.Key] = double.PositiveInfinity;
                        priorityQueue.Enqueue(node.Key,double.PositiveInfinity);
                    }
                    _predecessors[node.Key] = null;
                }

                HashSet<Node<T>> visited = new HashSet<Node<T>>();

                while(priorityQueue.Count > 0){
                    Node<T> node = priorityQueue.Dequeue();

                    if(!visited.Contains(node)){
                        visited.Add(node);
                        foreach (var (neighbor, weight) in map.AdjacencyList[node]){
                            if (visited.Contains(neighbor))
                                continue;

                            double newDistance = _distances[node] + weight;
                            if (newDistance < _distances[neighbor])
                            {
                                _distances[neighbor] = newDistance;
                                _predecessors[neighbor] = node;
                                priorityQueue.Enqueue(neighbor, newDistance);
                            }
                        }
                    }
                }
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