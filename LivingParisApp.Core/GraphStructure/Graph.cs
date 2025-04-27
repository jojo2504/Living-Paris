namespace LivingParisApp.Core.GraphStructure {
    public abstract class Graph<T> {
        public Dictionary<Node<T>, int> nodeToIndexMap = [];
        public Dictionary<Node<T>, List<Tuple<Node<T>, double>>> AdjacencyList { get; private set; } = [];
        public int?[,] AdjacencyMatrix { get; private set; }

        public void AddEdge(T from, T to, double weight = 0) {
            var fromNode = new Node<T>(from);
            var toNode = new Node<T>(to);

            // Ensure both nodes exist in the adjacency list
            if (!AdjacencyList.ContainsKey(fromNode))
                AdjacencyList[fromNode] = new List<Tuple<Node<T>, double>>();
            if (!AdjacencyList.ContainsKey(toNode))
                AdjacencyList[toNode] = new List<Tuple<Node<T>, double>>();

            // Add the edge
            AdjacencyList[fromNode].Add(Tuple.Create(toNode, weight));
        }

        public void AddBidirectionalEdge(T A, T B, double weight = 0) {
            var Anode = new Node<T>(A);
            var Bnode = new Node<T>(B);

            // Ensure both nodes exist in the adjacency list
            if (!AdjacencyList.ContainsKey(Anode))
                AdjacencyList[Anode] = new List<Tuple<Node<T>, double>>();
            if (!AdjacencyList.ContainsKey(Bnode))
                AdjacencyList[Bnode] = new List<Tuple<Node<T>, double>>();

            // Add the edge
            AdjacencyList[Anode].Add(Tuple.Create(Bnode, weight));
            AdjacencyList[Bnode].Add(Tuple.Create(Anode, weight));
        }
        
        public abstract override string ToString();
    }
}