using LivingParisApp.Core.GraphStructure;

namespace LivingParisApp.Core.Engines.GraphColoration {
    public class VertexCover<T> : Graph<T> {
        public HashSet<Node<T>> GetApproximateVertexCover() {
            var visitedEdges = new HashSet<(Node<T>, Node<T>)>();
            var vertexCover = new HashSet<Node<T>>();

            // Work on a copy to avoid modifying the original structure
            var edges = new List<(Node<T>, Node<T>)>();

            foreach (var fromNode in AdjacencyList) {
                foreach (var (toNode, _) in fromNode.Value) {
                    var edge = (fromNode.Key, toNode);
                    var reverseEdge = (toNode, fromNode.Key);

                    if (!visitedEdges.Contains(edge) && !visitedEdges.Contains(reverseEdge)) {
                        edges.Add(edge);
                        visitedEdges.Add(edge);
                        visitedEdges.Add(reverseEdge);
                    }
                }
            }

            visitedEdges.Clear();

            foreach (var (u, v) in edges) {
                if (!visitedEdges.Contains((u, v)) && !visitedEdges.Contains((v, u))) {
                    vertexCover.Add(u);
                    vertexCover.Add(v);

                    // Mark all edges incident to u and v as visited
                    foreach (var neighbor in AdjacencyList[u])
                        visitedEdges.Add((u, neighbor.Item1));
                    foreach (var neighbor in AdjacencyList[v])
                        visitedEdges.Add((v, neighbor.Item1));
                }
            }

            return vertexCover;
        }

        public override string ToString() {
            throw new NotImplementedException();
        }
    }
}