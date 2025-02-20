using System.Text;
using LivingParisApp.Services.FileHandling;
using LivingParisApp.Services.Logging;

namespace LivingParisApp.Core.GraphStructure {
    public class Graph<T> {
        public Dictionary<Node<T>, List<Tuple<Node<T>, int>>> AdjacencyList { get; private set; } = [];
        public int?[,] AdjacencyMatrix { get; private set; }
        // Track node-to-index mapping for the matrix
        private List<Node<T>> nodeIndex = new List<Node<T>>();
        private Dictionary<Node<T>, int> nodeToIndex = new Dictionary<Node<T>, int>();

        public void AddEdge(T value1, T value2, int weight) {
            // Create nodes with the given values
            var node1 = new Node<T>(value1);
            var node2 = new Node<T>(value2);

            // Check if a node with the same Object value already exists in the dictionary
            var existingNode1 = AdjacencyList.Keys.FirstOrDefault(n => n.Equals(node1));
            var existingNode2 = AdjacencyList.Keys.FirstOrDefault(n => n.Equals(node2));

            // If node1 doesn't exist in the dictionary, add it
            if (existingNode1 == null) {
                AdjacencyList[node1] = new List<Tuple<Node<T>, int>>();
                existingNode1 = node1;
                UpdateNodeIndex(existingNode1);
            }

            // If node2 doesn't exist in the dictionary, add it
            if (existingNode2 == null) {
                AdjacencyList[node2] = new List<Tuple<Node<T>, int>>();
                existingNode2 = node2;
                UpdateNodeIndex(existingNode2);
            }

            // Add the edge from node1 to node2 with the given weight
            AdjacencyList[existingNode1].Add(Tuple.Create(existingNode2, weight));
            AdjacencyList[existingNode2].Add(Tuple.Create(existingNode1, weight));

            // Update AdjacencyMatrix
            UpdateAdjacencyMatrix(existingNode1, existingNode2, weight);
        }

        private void UpdateNodeIndex(Node<T> node) {
            if (!nodeToIndex.ContainsKey(node)) {
                nodeIndex.Add(node);
                nodeToIndex[node] = nodeIndex.Count - 1;
                ResizeMatrix();
            }
        }

        private void ResizeMatrix() {
            int newSize = nodeIndex.Count;
            int?[,] newMatrix = new int?[newSize, newSize];

            // Copy existing data
            if (AdjacencyMatrix != null) {
                int oldSize = AdjacencyMatrix.GetLength(0);
                for (int i = 0; i < oldSize; i++) {
                    for (int j = 0; j < oldSize; j++) {
                        newMatrix[i, j] = AdjacencyMatrix[i, j];
                    }
                }
            }

            AdjacencyMatrix = newMatrix;
        }

        private void UpdateAdjacencyMatrix(Node<T> fromNode, Node<T> toNode, int weight) {
            int fromIndex = nodeToIndex[fromNode];
            int toIndex = nodeToIndex[toNode];

            // Undirected graph: symmetric matrix
            AdjacencyMatrix[fromIndex, toIndex] = weight;
            AdjacencyMatrix[toIndex, fromIndex] = weight;
        }

        public override string ToString() {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<Node<T>, List<Tuple<Node<T>, int>>> kpv in AdjacencyList) {
                stringBuilder.Append($"{kpv.Key.Object} => ");
                foreach (var node in kpv.Value) {
                    stringBuilder.Append($"{node.Item1.Object.ToString()}({node.Item2}), ");
                }
                stringBuilder.Length -= 2;
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEnd();
        }
    }
}