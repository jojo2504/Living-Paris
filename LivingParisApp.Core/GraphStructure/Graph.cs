using System.Text;
using LivingParisApp.Services.FileHandling;
using LivingParisApp.Services.Logging;

namespace LivingParisApp.Core.GraphStructure {
    public class Graph<T> {
        private List<Link<T>> _links;
        public Dictionary<Node<T>, List<Tuple<Node<T>, double>>> AdjacencyList { get; private set; } = [];
        public int?[,] AdjacencyMatrix { get; private set; }

        // Constructor accepting optional links for flexibility
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

        // Optional constructor for file-based initialization (for soc-karate.mtx)
        public Graph(Func<string, T> converter) {
            Logger.Log("Initializing the graph from file");
            try {
                _links = GetAllLinks(converter);
                CreateAdjacencies();
                Logger.Log($"{AdjacencyList.Count()} nodes in AdjacencyList");
                Logger.Success("Graph initialized from file");
            }
            catch (Exception ex) {
                Logger.Log(ex.ToString());
            }
        }

        public void AddEdge(Link<T> link) {
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

        private void CreateAdjacencies() {
            Logger.Log("Creating Adjacencies...");
            Logger.Important($"{_links.Count()}");
            foreach (var link in _links) {
                AddEdge(link);
            }
        }

        private List<Link<T>> GetAllLinks(Func<string, T> converter) {
            Logger.Log("Getting All Links from file");
            var links = new List<Link<T>>();
            try {
                FileReader fileReader = new FileReader("C:/Users/jojod/Documents/c#/Living-Paris/assets/soc-karate.mtx");
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
    }
}