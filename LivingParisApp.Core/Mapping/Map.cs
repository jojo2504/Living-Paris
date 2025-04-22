using System.Text;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Services.FileHandling;

namespace LivingParisApp.Core.Mapping {
    public class Map<T> : Graph<T> where T : IStation<T> {
        private Dictionary<string, Node<T>> stationIdToNode = new Dictionary<string, Node<T>>();
        private Dictionary<string, Node<T>> stationNameToNode = new Dictionary<string, Node<T>>();

        public Map() {
        }

        public Map(string filePathNode, string filePathArcs) {
            ParseNodes(filePathNode);
            ParseArcs(filePathArcs);
        }

        public override void AddEdge(T from, T to, double weight = 0) {
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

        public override void AddBidirectionalEdge(T A, T B, double weight = 0){
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

        public override void ParseNodes(string filePathNode) {
            // Step 1: Parse nodes (stations) from the first file
            FileReader nodeInitReader = new FileReader(filePathNode);
            
            // First pass: collect all stations and map station names to nodes
            foreach (string line in nodeInitReader.ReadLines()) {
                var parts = line.Split(',');
                if (parts.Length < 7 || parts[0] == "ID Station") continue; // Skip header or malformed lines

                T station = T.FromParts(parts);
                string stationId = parts[0];
                string stationName = station.LibelleStation;
                
                var node = new Node<T>(station);
                
                // Store the node by ID
                stationIdToNode[stationId] = node;
                
                // Store the node by name (keeping only one node per station name)
                if (!stationNameToNode.ContainsKey(stationName)) {
                    stationNameToNode[stationName] = node;
                }
            }
            
            // Now add all unique nodes to the adjacency list
            foreach (var node in stationNameToNode.Values) {
                if (!AdjacencyList.ContainsKey(node)) {
                    AdjacencyList[node] = new List<Tuple<Node<T>, double>>();
                }
            }
        }

        public override void ParseArcs(string filePathArcs) {
            // Step 2: Parse arcs file and build the adjacency list
            FileReader arcInitReader = new FileReader(filePathArcs);

            // Create a set to track connections we've already made
            var processedConnections = new HashSet<string>();

            foreach (string line in arcInitReader.ReadLines()) {
                var parts = line.Split(',');
                if (parts.Length < 6 || parts[0] == "Station Id") continue; // Skip header or malformed lines

                string id = parts[0];              // Current station ID
                string previousId = parts[2];      // Précédent (previous station ID)
                string nextId = parts[3];          // Suivant (next station ID)
                double travelTime = string.IsNullOrEmpty(parts[4]) ? 0 : Convert.ToDouble(parts[4]); // Time from current to next or previous
                double changeTime = string.IsNullOrEmpty(parts[5]) ? 0 : Convert.ToDouble(parts[5]);

                // Skip if station not in nodes file
                if (!stationIdToNode.ContainsKey(id)) continue;
                
                // Get the canonical node by station name
                var currentStationName = stationIdToNode[id].Object.LibelleStation;
                var currentNode = stationNameToNode[currentStationName];

                // Process connection to previous station
                if (!string.IsNullOrEmpty(previousId) && stationIdToNode.ContainsKey(previousId)) {
                    var prevStationName = stationIdToNode[previousId].Object.LibelleStation;
                    var prevNode = stationNameToNode[prevStationName];

                    // Create a unique connection identifier using station names
                    string connectionId = string.Compare(currentStationName, prevStationName) < 0
                        ? $"{currentStationName}-{prevStationName}"
                        : $"{prevStationName}-{currentStationName}";

                    // Only add if we haven't processed this connection yet
                    if (!processedConnections.Contains(connectionId)) {
                        // Add bidirectional connection
                        AdjacencyList[currentNode].Add(Tuple.Create(prevNode, travelTime + changeTime));
                        AdjacencyList[prevNode].Add(Tuple.Create(currentNode, travelTime + changeTime));

                        processedConnections.Add(connectionId);
                    }
                }

                // Process connection to next station
                if (!string.IsNullOrEmpty(nextId) && stationIdToNode.ContainsKey(nextId)) {
                    var nextStationName = stationIdToNode[nextId].Object.LibelleStation;
                    var nextNode = stationNameToNode[nextStationName];

                    // Create a unique connection identifier using station names
                    string connectionId = string.Compare(currentStationName, nextStationName) < 0
                        ? $"{currentStationName}-{nextStationName}"
                        : $"{nextStationName}-{currentStationName}";

                    // Only add if we haven't processed this connection yet
                    if (!processedConnections.Contains(connectionId)) {
                        // Add bidirectional connection
                        AdjacencyList[currentNode].Add(Tuple.Create(nextNode, travelTime + changeTime));
                        AdjacencyList[nextNode].Add(Tuple.Create(currentNode, travelTime + changeTime));

                        processedConnections.Add(connectionId);
                    }
                }
            }
        }

        public override string ToString() {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<Node<T>, List<Tuple<Node<T>, double>>> kpv in AdjacencyList) {
                stringBuilder.Append($"{kpv.Key.Object.LibelleStation} => ");
                foreach (var node in kpv.Value) {
                    stringBuilder.Append($"{node.Item1.Object.LibelleStation}({node.Item2}), ");
                }
                if (kpv.Value.Count > 0) {
                    stringBuilder.Length -= 2; // Remove trailing comma and space
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEnd();
        }
    }
}