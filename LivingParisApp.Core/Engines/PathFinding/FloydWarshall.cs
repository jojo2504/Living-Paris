using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths {
    public class FloydWarshall<T> : ShortestPathsEngine<T> where T : IStation {
        private int vertexCount;        // Number of vertices
        private List<Node<T>> vertices; // List of vertices for indexing

        public override void Init(Map<T> map, T? first = default) {
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

        public override (LinkedList<Node<T>> Path, double TotalLength) GetPath(Node<T> from, Node<T> to) {
            int startIndex = vertices.IndexOf(from);
            int endIndex = vertices.IndexOf(to);

            // If no path exists, return an empty LinkedList and infinity as TotalLength
            if (distances[startIndex, endIndex] == double.PositiveInfinity) {
                return (new LinkedList<Node<T>>(), double.PositiveInfinity);
            }

            // Build the path as a List first (for easier insertion), then convert to LinkedList
            List<Node<T>> pathList = new List<Node<T>>();
            pathList.Add(to); // Start with the destination node

            while (startIndex != endIndex) {
                endIndex = predecessors[startIndex, endIndex];
                pathList.Insert(0, vertices[endIndex]); // Insert at the beginning to reverse the path
            }

            // Convert List to LinkedList
            LinkedList<Node<T>> path = new LinkedList<Node<T>>(pathList);

            // Return the path and the total length from the distances matrix
            return (path, distances[startIndex, vertices.IndexOf(to)]);
        }
    }
}