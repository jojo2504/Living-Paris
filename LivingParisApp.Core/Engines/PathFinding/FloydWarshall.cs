using LivingParisApp.Core.GraphStructure;

namespace LivingParisApp.Core.Engines.ShortestPaths{
    public class FloydWarshall<T>
    {
        private double[,] distances;    // Distance matrix
        private int[,] predecessors;    // Predecessor matrix
        private int vertexCount;        // Number of vertices
        private List<Node<T>> vertices; // List of vertices for indexing

        /// <summary>
        /// Initializes a new instance of the Floyd-Warshall algorithm with a given graph.
        /// </summary>
        /// <param name="graph">Dictionary representing the graph with nodes and their weighted neighbors.</param>
        public FloydWarshall(Dictionary<Node<T>, List<Tuple<Node<T>, double>>> graph)
        {
            vertices = graph.Keys.ToList();
            vertexCount = vertices.Count;
            distances = new double[vertexCount, vertexCount];
            predecessors = new int[vertexCount, vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    if (i == j)
                    {
                        distances[i, j] = 0;
                        predecessors[i, j] = -1; 
                    }
                    else
                    {
                        distances[i, j] = double.PositiveInfinity;
                        predecessors[i, j] = -1; 
                    }
                }
            }

            foreach (var node in graph)
            {
                int i = vertices.IndexOf(node.Key);
                foreach (var neighbor in node.Value)
                {
                    int j = vertices.IndexOf(neighbor.Item1);
                    distances[i, j] = neighbor.Item2; 
                    predecessors[i, j] = i;           
                }
            }

            for (int k = 0; k < vertexCount; k++)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    for (int j = 0; j < vertexCount; j++)
                    {
                        if (distances[i, k] != double.PositiveInfinity && 
                            distances[k, j] != double.PositiveInfinity && 
                            distances[i, k] + distances[k, j] < distances[i, j])
                        {
                            distances[i, j] = distances[i, k] + distances[k, j];
                            predecessors[i, j] = predecessors[k, j]; 
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reconstructs the shortest path between two nodes using the predecessor matrix.
        /// </summary>
        /// <param name="start">Starting node.</param>
        /// <param name="end">Ending node.</param>
        /// <returns>A list of nodes forming the shortest path from start to end, or an empty list if no path exists.</returns>
        public List<Node<T>> GetPath(Node<T> start, Node<T> end)
        {
            int startIndex = vertices.IndexOf(start);
            int endIndex = vertices.IndexOf(end);

            if (distances[startIndex, endIndex] == double.PositiveInfinity) 
                return new List<Node<T>>();

            List<Node<T>> path = new List<Node<T>>();
            path.Add(end); 

            while (startIndex != endIndex)
            {
                endIndex = predecessors[startIndex, endIndex];
                path.Insert(0, vertices[endIndex]); 
            }

            return path;
        }
    }
}