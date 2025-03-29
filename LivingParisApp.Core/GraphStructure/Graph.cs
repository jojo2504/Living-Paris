using System.Text;
using LivingParisApp.Services.FileHandling;
using LivingParisApp.Services.Logging;

namespace LivingParisApp.Core.GraphStructure {
    public abstract class Graph<T> {
        // private readonly List<Link<T>> _links;
        public Dictionary<Node<T>, int> nodeToIndexMap = [];
        public Dictionary<Node<T>, List<Tuple<Node<T>, double>>> AdjacencyList { get; private set; } = [];
        public int?[,] AdjacencyMatrix { get; private set; }

        //cette region est pour le premier rendu, rien ne sera reutilise
        #region 
        /*
        /// <summary>
        /// Constructor accepting optional links for flexibility
        /// </summary>
        /// <param name="links"></param>
        public Graph(IEnumerable<Link<T>> links = null) {
            Logger.Log("Initializing the graph with existing? link");
            try {
                _links = links?.ToList() ?? new List<Link<T>>();
                CreateAdjacencies();
                Logger.Log($"{AdjacencyList.Count()} nodes in AdjacencyList");
                Logger.Success("Graph initialized");
            }
            catch (Exception ex) {
                Logger.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Optional constructor for file-based initialization (for soc-karate.mtx)
        /// </summary>
        /// <param name="converter"></param>
        /// <param name="filePath"></param>
        public Graph(Func<string, T> converter, string filePath) {
            Logger.Log("Initializing the graph from file");
            try {
                _links = GetAllLinks(converter, filePath);
                CreateAdjacencies();
                Logger.Log($"{AdjacencyList.Count()} nodes in AdjacencyList");
                Logger.Success("Graph initialized from file");
            }
            catch (Exception ex) {
                Logger.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Adds a link to the adjacency list of the graph.
        /// </summary>
        /// <param name="link">The link to be added between two nodes.</param>
        public void AddEdgeList(Link<T> link) {
            var A = link.A;
            var B = link.B;
            var existingNodeA = AdjacencyList.Keys.FirstOrDefault(n => n.Equals(A));
            var existingNodeB = AdjacencyList.Keys.FirstOrDefault(n => n.Equals(B));

            existingNodeA = existingNodeA ?? A;
            existingNodeB = existingNodeB ?? B;

            if (!AdjacencyList.ContainsKey(existingNodeA))
                AdjacencyList[existingNodeA] = new List<Tuple<Node<T>, double>>();
            if (!AdjacencyList.ContainsKey(existingNodeB))
                AdjacencyList[existingNodeB] = new List<Tuple<Node<T>, double>>();

            if (link.Direction == Direction.Undirected || link.Direction == Direction.Direct) {
                AdjacencyList[existingNodeA].Add(Tuple.Create(existingNodeB, link.Weight));
            }

            if (link.Direction == Direction.Undirected || link.Direction == Direction.Indirect) {
                AdjacencyList[existingNodeB].Add(Tuple.Create(existingNodeA, link.Weight));
            }
        }

        /// <summary>
        /// Adds a link to the adjacency matrix of the graph.
        /// </summary>
        /// <param name="link">The link to be added between two nodes.</param>
        public void AddEdgeMatrix(Link<T> link) {
            Node<T> fromNode = link.A;
            int fromIndex = nodeToIndexMap[fromNode];

            Node<T> toNode = link.B;
            double weight = link.Weight;
            int toIndex = nodeToIndexMap[toNode];

            AdjacencyMatrix[fromIndex, toIndex] = (int)Math.Round(weight);
        }

        /// <summary>
        /// Creates adjacency structures (list and matrix) from existing links.
        /// </summary>
        private void CreateAdjacencies() {
            Logger.Log("Creating Adjacencies...");
            Logger.Important($"{_links.Count()}");
            try {
                Logger.Log("Creating Adjacency List");
                foreach (var link in _links) {
                    AddEdgeList(link);
                }
                for (int i = 0; i < AdjacencyList.Count(); i++) {
                    nodeToIndexMap[AdjacencyList.Keys.ToList()[i]] = i;
                }
                Logger.Success("");
            }
            catch (Exception ex) {
                Logger.Log(ex.Message);
            }
            try {
                var nodeCount = AdjacencyList.Count();
                AdjacencyMatrix = new int?[nodeCount, nodeCount];
                for (int i = 0; i < nodeCount; i++) {
                    for (int j = 0; j < nodeCount; j++) {
                        AdjacencyMatrix[i, j] = 0;
                    }
                }

                Logger.Log("Creating Adjacency Matrix");
                foreach (var link in _links) {
                    AddEdgeMatrix(link);
                }
                Logger.Success("");
            }
            catch (Exception ex) {
                Logger.Log(ex.Message);
            }
        }

        /// <summary>
        /// Loads all links from a given file.
        /// </summary>
        /// <param name="converter">Function to convert a string to generic type T.</param>
        /// <param name="filePath">Path to the file containing the links.</param>
        /// <returns>List of links extracted from the file.</returns>
        private List<Link<T>> GetAllLinks(Func<string, T> converter, string filePath = "") {
            Logger.Log("Getting All Links from file");
            var links = new List<Link<T>>();
            try {
                FileReader fileReader = new FileReader(filePath);
                foreach (string line in fileReader.ReadLines()) {
                    var parts = line.Split(' ');
                    if (!double.TryParse(parts[0], out _)) continue;

                    T nodeA = converter(parts[0]);
                    T nodeB = converter(parts[1]);

                    if (parts.Length != 3) {
                        links.Add(new Link<T>(new Node<T>(nodeA), new Node<T>(nodeB), Direction.Undirected, 1));
                    }
                    else {
                        double weight = double.Parse(parts[2]);
                        links.Add(new Link<T>(new Node<T>(nodeA), new Node<T>(nodeB), Direction.Undirected, weight));
                    }
                }
                Logger.Success("Links loaded from file");
            }
            catch (Exception ex) {
                Logger.Fatal(ex);
            }
            return links;
        }

        /// <summary>
        /// Returns a string representation of the adjacency list.
        /// </summary>
        /// <returns>String representing the adjacency list.</returns>
        public override string ToString() {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<Node<T>, List<Tuple<Node<T>, double>>> kpv in AdjacencyList) {
                stringBuilder.Append($"{kpv.Key.Object} => ");
                foreach (var node in kpv.Value) {
                    stringBuilder.Append($"{node.Item1.Object}({node.Item2}), ");
                }
                stringBuilder.Length -= 2;
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEnd();
        }

        /// <summary>
        /// Displays the adjacency matrix as a string.
        /// </summary>
        /// <returns>String representing the adjacency matrix.</returns>
        public string DisplayAdjacencyMatrix() {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < AdjacencyMatrix.GetLength(0); i++) {
                for (int j = 0; j < AdjacencyMatrix.GetLength(1); j++) {
                    stringBuilder.Append($"{AdjacencyMatrix[i, j]} ");
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEnd();
        }
        */
        #endregion

        public abstract void ParseNodes(string filePathNode);
        public abstract void ParseArcs(string filePathArcs);
        public abstract override string ToString();
    }
}