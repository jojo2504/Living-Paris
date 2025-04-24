using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingParisApp.Core.Engines.GraphColoration {
    public class WelshPowell<T> {
        private RelationshipMap<T> _map;
        private Dictionary<Node<T>, int> _colorAssignment;

        public WelshPowell(RelationshipMap<T> map) {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _colorAssignment = new Dictionary<Node<T>, int>();
        }

        public Dictionary<Node<T>, int> ColorGraph() {
            // Clear any existing color assignments
            _colorAssignment.Clear();

            // Sort nodes by degree (number of connections) in descending order
            var sortedNodes = _map.AdjacencyList.Keys
                .OrderByDescending(node => _map.AdjacencyList[node].Count)
                .ToList();

            // Initialize with no colors assigned
            foreach (var node in sortedNodes) {
                _colorAssignment[node] = -1; // -1 indicates no color assigned
            }

            int colorCount = 0;

            // Assign colors to all nodes
            foreach (var currentNode in sortedNodes) {
                // Skip if node is already colored
                if (_colorAssignment[currentNode] != -1) continue;

                // Try to assign the current color
                int currentColor = colorCount;

                // Check if current color can be assigned
                if (CanAssignColor(currentNode, currentColor)) {
                    _colorAssignment[currentNode] = currentColor;

                    // Try to assign the same color to other non-adjacent nodes
                    foreach (var node in sortedNodes) {
                        if (_colorAssignment[node] == -1 && CanAssignColor(node, currentColor)) {
                            _colorAssignment[node] = currentColor;
                        }
                    }

                    // Increment color count for next iteration
                    colorCount++;
                }
            }

            return new Dictionary<Node<T>, int>(_colorAssignment);
        }

        private bool CanAssignColor(Node<T> node, int color) {
            // Check all adjacent nodes
            if (!_map.AdjacencyList.ContainsKey(node)) return true;

            foreach (var adjacentNodeTuple in _map.AdjacencyList[node]) {
                var adjacentNode = adjacentNodeTuple.Item1;

                // If adjacent node has same color, we can't assign this color
                if (_colorAssignment.ContainsKey(adjacentNode) &&
                    _colorAssignment[adjacentNode] == color) {
                    return false;
                }
            }

            // No conflicts found, color can be assigned
            return true;
        }

        public int GetColorCount() {
            if (_colorAssignment.Count == 0) {
                ColorGraph(); // If not colored yet, color the graph
            }

            return _colorAssignment.Values.DefaultIfEmpty(-1).Max() + 1;
        }

        public Dictionary<int, List<Node<T>>> GetNodesGroupedByColor() {
            if (_colorAssignment.Count == 0) {
                ColorGraph(); // If not colored yet, color the graph
            }

            var result = new Dictionary<int, List<Node<T>>>();

            foreach (var kvp in _colorAssignment) {
                if (!result.ContainsKey(kvp.Value)) {
                    result[kvp.Value] = new List<Node<T>>();
                }

                result[kvp.Value].Add(kvp.Key);
            }

            return result;
        }

        public bool IsBipartite() {
            if (_colorAssignment.Count == 0) {
                ColorGraph();
            }

            return GetColorCount() <= 2;
        }

        public bool IsPlanar() {
            // Get number of vertices (V) and edges (E)
            int V = _map.AdjacencyList.Keys.Count;
            int E = _map.AdjacencyList.Sum(adj => adj.Value.Count) / 2; // Divide by 2 as edges are counted twice

            // Empty or single vertex graphs are planar
            if (V <= 2) return true;

            // Check edge count against planar graph maximum (3V - 6 for V ≥ 3)
            if (E > 3 * V - 6) return false;

            // Check for K5 (5 vertices all connected to each other)
            var nodes = _map.AdjacencyList.Keys.ToList();
            for (int i = 0; i < V - 4; i++) {
                for (int j = i + 1; j < V - 3; j++) {
                    for (int k = j + 1; k < V - 2; k++) {
                        for (int l = k + 1; l < V - 1; l++) {
                            for (int m = l + 1; m < V; m++) {
                                if (FormsK5(nodes[i], nodes[j], nodes[k], nodes[l], nodes[m])) {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // Check for K3,3 (bipartite with 3 vertices in each set, all connected)
            if (IsBipartite()) {
                var colorGroups = GetNodesGroupedByColor();
                if (colorGroups.Count == 2) {
                    var group1 = colorGroups[0];
                    var group2 = colorGroups[1];
                    if (group1.Count >= 3 && group2.Count >= 3) {
                        // Check if any 3 nodes in group1 are all connected to any 3 nodes in group2
                        for (int i = 0; i < group1.Count - 2; i++) {
                            for (int j = i + 1; j < group1.Count - 1; j++) {
                                for (int k = j + 1; k < group1.Count; k++) {
                                    for (int x = 0; x < group2.Count - 2; x++) {
                                        for (int y = x + 1; y < group2.Count - 1; y++) {
                                            for (int z = y + 1; z < group2.Count; z++) {
                                                if (FormsK33(group1[i], group1[j], group1[k], group2[x], group2[y], group2[z])) {
                                                    return false;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool FormsK5(Node<T> n1, Node<T> n2, Node<T> n3, Node<T> n4, Node<T> n5) {
            var nodes = new[] { n1, n2, n3, n4, n5 };
            for (int i = 0; i < 5; i++) {
                for (int j = i + 1; j < 5; j++) {
                    if (!AreConnected(nodes[i], nodes[j])) {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool FormsK33(Node<T> a1, Node<T> a2, Node<T> a3, Node<T> b1, Node<T> b2, Node<T> b3) {
            var setA = new[] { a1, a2, a3 };
            var setB = new[] { b1, b2, b3 };
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    if (!AreConnected(setA[i], setB[j])) {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool AreConnected(Node<T> node1, Node<T> node2) {
            if (!_map.AdjacencyList.ContainsKey(node1) || !_map.AdjacencyList.ContainsKey(node2)) {
                return false;
            }
            return _map.AdjacencyList[node1].Any(tuple => tuple.Item1.Equals(node2));
        }

        public void Reset() {
            _colorAssignment.Clear();
        }
    }
}