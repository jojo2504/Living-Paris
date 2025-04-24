using LivingParisApp.Core.Entities.Station;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths {
    public class FloydWarshall<T> : ShortestPathsEngine<T> where T : IStation<T> {
        private int vertexCount;     
        private List<Node<T>> vertices; 

        /// <summary>
        /// Initializes the algorithm with a map and computes all shortest paths.
        /// </summary>
        /// <param name="map">The map containing nodes and their connections</param>
        public void Init(Map<T> map) {

            vertices = map.AdjacencyList.Keys.ToList();
            vertexCount = vertices.Count;
            distances = new double[vertexCount, vertexCount];
            predecessors = new int[vertexCount, vertexCount];

            for (int i = 0; i < vertexCount; i++) {
                for (int j = 0; j < vertexCount; j++) {
                    if (i == j) {
                        distances[i, j] = 0;
                        predecessors[i, j] = -1;
                    }
                    else {
                        distances[i, j] = double.PositiveInfinity;
                        predecessors[i, j] = -1;
                    }
                }
            }

            foreach (var node in map.AdjacencyList) {
                int i = vertices.IndexOf(node.Key);
                foreach (var neighbor in node.Value) {
                    int j = vertices.IndexOf(neighbor.Item1);
                    distances[i, j] = neighbor.Item2;
                    predecessors[i, j] = i;
                }
            }

            for (int k = 0; k < vertexCount; k++) {
                for (int i = 0; i < vertexCount; i++) {
                    for (int j = 0; j < vertexCount; j++) {
                        if (distances[i, k] != double.PositiveInfinity &&
                            distances[k, j] != double.PositiveInfinity &&
                            distances[i, k] + distances[k, j] < distances[i, j]) {
                            distances[i, j] = distances[i, k] + distances[k, j];
                            predecessors[i, j] = predecessors[k, j];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the shortest path between two nodes and its total length.
        /// </summary>
        /// <param name="from">The starting node</param>
        /// <param name="to">The destination node</param>
        /// <returns>A tuple containing the path (LinkedList) and total length</returns>
        public (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> from, Node<T> to) {
            int startIndex = vertices.IndexOf(from);
            int endIndex = vertices.IndexOf(to);

            if (distances[startIndex, endIndex] == double.PositiveInfinity) {
                return (new LinkedList<Node<T>>(), double.PositiveInfinity);
            }

            List<Node<T>> pathList = new List<Node<T>>();
            pathList.Add(to); 

            while (startIndex != endIndex) {
                endIndex = predecessors[startIndex, endIndex];
                pathList.Insert(0, vertices[endIndex]); 
            }

            LinkedList<Node<T>> path = new LinkedList<Node<T>>(pathList);

            return (path, distances[startIndex, vertices.IndexOf(to)]);
        }
    }
}