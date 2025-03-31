using System.Text;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Services.FileHandling;

namespace LivingParisApp.Core.Mapping {
    public class Map<T> : Graph<T> where T : IStation {
        private Dictionary<string, Node<T>> stationNodes = new Dictionary<string, Node<T>>();
        
        public Map(){
        }

        public Map(string filePathNode, string filePathArcs) {
            ParseNodes(filePathNode);
            ParseArcs(filePathArcs);
        }

        public override void AddEdge(T from, T to, double weight) {
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

        public override void ParseNodes(string filePathNode){
            // Step 1: Parse nodes (stations) from the first file
            FileReader nodeInitReader = new FileReader(filePathNode);
            foreach (string line in nodeInitReader.ReadLines()) {
                var parts = line.Split(',');
                if (parts.Length < 7 || parts[0] == "ID Station") continue; // Skip header or malformed lines

                string id = parts[0];
                string lineLabel = parts[1];
                string stationLabel = parts[2];
                double longitude = string.IsNullOrEmpty(parts[3]) ? 0 : Convert.ToDouble(parts[3]);
                double latitude = string.IsNullOrEmpty(parts[4]) ? 0 : Convert.ToDouble(parts[4]);
                string communeName = parts[5];
                string communeCode = parts[6];

                var station = new {Id = Convert.ToInt32(id), lineLabel, stationLabel, longitude,
                                                latitude, communeName, communeCode};
                stationNodes[id] = new Node<T>((T)(object)station); //pas fou mais jsp comment faire d'autre
            }
        }

        public override void ParseArcs(string filePathArcs) {
            // Step 2: Parse arcs file and build the adjacency list
            FileReader arcInitReader = new FileReader(filePathArcs);
            var stationTravelTimes = new Dictionary<string, double>(); // Store travel times from current to next

            foreach (string line in arcInitReader.ReadLines()) {
                var parts = line.Split(',');
                if (parts.Length < 6 || parts[0] == "Station Id") continue; // Skip header or malformed lines

                string id = parts[0];              // Current station ID
                string previousId = parts[2];      // Précédent (previous station ID)
                string nextId = parts[3];          // Suivant (next station ID)
                double travelTime = string.IsNullOrEmpty(parts[4]) ? 0 : Convert.ToDouble(parts[4]); // Time from current to next or previous
                double changeTime = string.IsNullOrEmpty(parts[5]) ? 0 : Convert.ToDouble(parts[5]);

                if (!stationNodes.ContainsKey(id)) continue; // Skip if station not in nodes file
                var currentNode = stationNodes[id];

                // Initialize neighbors list if not already present
                if (!AdjacencyList.ContainsKey(currentNode)) {
                    AdjacencyList[currentNode] = new List<Tuple<Node<T>, double>>();
                }

                // Add edge to Previous station (bidirectional)
                if (!string.IsNullOrEmpty(previousId) && stationNodes.ContainsKey(previousId)) {
                    var prevNode = stationNodes[previousId];
                    AdjacencyList[currentNode].Add(Tuple.Create(prevNode, travelTime+changeTime)); // Use current travelTime for symmetry
                }

                // Add edge to Next station
                if (!string.IsNullOrEmpty(nextId) && stationNodes.ContainsKey(nextId)) {
                    var nextNode = stationNodes[nextId];
                    AdjacencyList[currentNode].Add(Tuple.Create(nextNode, travelTime+changeTime));
                    // Store travel time from current to next for reverse edge
                    stationTravelTimes[id] = travelTime+changeTime;
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
                stringBuilder.Length -= 2;
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEnd();
        }
    }
}