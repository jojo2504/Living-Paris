using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingParisApp.Core.Engines.GraphColoration {
    public class WelshPowell<T> where T : IStation<T> {
        private Map<T> _map;
        private Dictionary<Node<T>, int> _colorAssignment;

        public WelshPowell(Map<T> map) {
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

        public void Reset() {
            _colorAssignment.Clear();
        }
    }
}