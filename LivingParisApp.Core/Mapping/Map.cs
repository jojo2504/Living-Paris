using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using LivingParisApp.Core.Entities.Station;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Services.FileHandling;
using LivingParisApp.Services.Logging;

namespace LivingParisApp.Core.Mapping {
    public class Map<T> : Graph<T> where T : IStation<T> {
        // Dictionary to map station ID to its Node
        private readonly Dictionary<string, Node<T>> stationIdToNode = new Dictionary<string, Node<T>>();
        private readonly Dictionary<string, List<Node<T>>> seenNodes = [];

        // HashSet containing all nodes including duplicates for stations on multiple lines
        public HashSet<Node<T>> THashSet = new HashSet<Node<T>>();

        // Dictionary to store station links
        private HashSet<MetroStationLink<T>> stationLinks = new HashSet<MetroStationLink<T>>();

        public Map() {
        }

        public Map(string filePathNode, string filePathArcs) {
            ParseNodes(filePathNode);
            ParseArcs(filePathArcs);
        }

        public void ParseNodes(string filePathNode) {
            // Step 1: Parse nodes (stations) from the first file
            FileReader nodeInitReader = new FileReader(filePathNode);

            // First pass: collect all stations
            foreach (string line in nodeInitReader.ReadLines()) {
                var parts = line.Split(',');
                if (parts.Length < 7 || parts[0] == "ID Station") continue; // Skip header or malformed lines

                T station = T.FromParts(parts);
                string stationId = parts[0];

                var node = new Node<T>(station);
                THashSet.Add(node); // Add to THashSet (keeps all stations including those on multiple lines)

                // Store the node by ID - if station exists on multiple lines, we keep the last one
                // This is fine since we'll use THashSet to find line information later
                stationIdToNode[stationId] = node;
            }

            // Initialize adjacency list with all unique stations
            foreach (var node in THashSet) {
                AdjacencyList[node] = new List<Tuple<Node<T>, double>>();
            }
        }

        public void ParseArcs(string filePathArcs) {
            // Step 2: Parse arcs file and build station links
            FileReader arcInitReader = new FileReader(filePathArcs);
            stationLinks = new HashSet<MetroStationLink<T>>();
            HashSet<Tuple<Node<T>, Node<T>>> processedLink = [];

            foreach (string line in arcInitReader.ReadLines()) {
                var parts = line.Split(',');
                if (parts.Length < 6 || parts[0] == "Station Id") continue; // Skip header or malformed lines

                string id = parts[0];              // Current station ID
                string previousId = parts[2];      // Previous station ID
                string nextId = parts[3];          // Next station ID
                double travelTime = string.IsNullOrEmpty(parts[4]) ? 0 : Convert.ToDouble(parts[4]); // Time from current to next or previous
                double changeTime = string.IsNullOrEmpty(parts[5]) ? 0 : Convert.ToDouble(parts[5]);

                // Skip if station not in nodes file
                if (!stationIdToNode.ContainsKey(id)) {
                    Logger.Log(stationIdToNode[id].Object.ToString(), "doesnt exist");
                    continue;
                }

                var currentNode = stationIdToNode[id];
                var StationName = currentNode.Object.LibelleStation;
                if (seenNodes.ContainsKey(StationName)) {
                    CreateLinksWithAllSelf(seenNodes[StationName], currentNode);
                    seenNodes[StationName].Add(currentNode); // add self after all otherline self
                }   
                else {
                    Logger.Log("creating new :", StationName);
                    seenNodes.Add(StationName, new List<Node<T>>() { currentNode });
                }

                // Process connection to previous station
                if (!string.IsNullOrEmpty(previousId) && stationIdToNode.ContainsKey(previousId)) {
                    var prevNode = stationIdToNode[previousId];

                    var tupleLink = Tuple.Create(prevNode, currentNode);
                    if (!processedLink.Contains(tupleLink)) {
                        processedLink.Add(tupleLink);
                        // Find common line between these stations
                        string commonLine = FindCommonLine(currentNode, prevNode);

                        // Create bidirectional links
                        var weight = travelTime + changeTime;
                        CreateBidirectionalLinks(currentNode, prevNode, weight, commonLine);
                    }
                }

                // Process connection to next station
                if (!string.IsNullOrEmpty(nextId) && stationIdToNode.ContainsKey(nextId)) {
                    var nextNode = stationIdToNode[nextId];

                    var tupleLink = Tuple.Create(nextNode, currentNode);
                    if (!processedLink.Contains(tupleLink)) {
                        processedLink.Add(tupleLink);

                        // Find common line between these stations
                        string commonLine = FindCommonLine(currentNode, nextNode);

                        // Create bidirectional links
                        var weight = travelTime + changeTime;
                        CreateBidirectionalLinks(currentNode, nextNode, weight, commonLine);
                    }
                }
            }

            // Now populate the adjacency list with the links
            BuildAdjacencyListFromLinks();
        }

        private void CreateLinksWithAllSelf(List<Node<T>> stationsToLink, Node<T>? currentNode) {
            foreach (var node in stationsToLink) {
                CreateBidirectionalLinks(node, currentNode, 1);
            }
        }

        // Helper method to create bidirectional links between stations
        private void CreateBidirectionalLinks(Node<T> nodeA, Node<T> nodeB, double weight, string line = "") {
            // Create forward link (A and B)
            MetroStationLink<T> linkAandB = new MetroStationLink<T>(
                nodeA,
                nodeB,
                Direction.Bidirectional,
                weight,
                line
            );
            // Add to our collection of links
            stationLinks.Add(linkAandB);
            //stationLinks.Add(linkBtoA);
        }

        // Helper method to find a common line between two stations
        private string FindCommonLine(Node<T> stationA, Node<T> stationB) {
            var linesA = GetStationLines(stationA.Object.LibelleStation);
            var linesB = GetStationLines(stationB.Object.LibelleStation);

            // Find the common lines
            var commonLines = linesA.Intersect(linesB).ToList();

            if (commonLines.Count > 0) {
                // If there are multiple common lines, we take the first one
                // You could implement more sophisticated logic here if needed
                return commonLines[0];
            }

            // If no common line found (this shouldn't happen in a well-formed metro system)
            return "Unknown";
        }

        // Helper method to get all lines a station is on
        private List<string> GetStationLines(string stationName) {
            List<string> lines = new List<string>();

            // Look through THashSet for all instances of this station name
            foreach (var node in THashSet) {
                if (node.Object.LibelleStation == stationName) {
                    // Assuming IStation has a Line property - adjust this based on your implementation
                    string line = node.Object.LibelleLine;
                    lines.Add(line);
                }
            }

            return lines;
        }

        // Helper method to build the adjacency list from the links
        private void BuildAdjacencyListFromLinks() {
            // Clear existing adjacency list
            AdjacencyList.Clear();

            // Add all links to the adjacency list
            foreach (var link in stationLinks) {
                Node<T> fromNode = link.A;
                Node<T> toNode = link.B;
                double weight = link.Weight;

                // Ensure the from node is in the adjacency list
                if (!AdjacencyList.ContainsKey(fromNode)) {
                    AdjacencyList[fromNode] = new List<Tuple<Node<T>, double>>();
                }

                // Add the connection
                AddBidirectionalEdge(fromNode, toNode, 1);
            }   
        }

        public void AddBidirectionalEdge(T A, T B, double weight = 0, string line = "Unknown") {
            // Look for existing nodes or create new ones
            Node<T> Anode = null;
            Node<T> Bnode = null;

            // Try to find existing nodes first
            foreach (var node in AdjacencyList.Keys) {
                if (node.Object.ID == A.ID)
                    Anode = node;
                if (node.Object.ID == B.ID)
                    Bnode = node;
            }

            // Create new nodes if not found
            if (Anode == null) Anode = new Node<T>(A);
            if (Bnode == null) Bnode = new Node<T>(B);

            // Create the links
            CreateBidirectionalLinks(Anode, Bnode, weight, line);

            // Update the adjacency list
            if (!AdjacencyList.ContainsKey(Anode))
                AdjacencyList[Anode] = new List<Tuple<Node<T>, double>>();
            if (!AdjacencyList.ContainsKey(Bnode))
                AdjacencyList[Bnode] = new List<Tuple<Node<T>, double>>();

            // Add the edges
            AdjacencyList[Anode].Add(Tuple.Create(Bnode, weight));
            AdjacencyList[Bnode].Add(Tuple.Create(Anode, weight));
        }

        public override string ToString() {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<Node<T>, List<Tuple<Node<T>, double>>> kpv in AdjacencyList) {
                stringBuilder.Append($"{kpv.Key.Object.LibelleStation}({kpv.Key.Object.LibelleLine}) => ");
                foreach (var node in kpv.Value) {
                    stringBuilder.Append($"{node.Item1.Object.LibelleStation}({node.Item1.Object.LibelleLine}), ");
                }
                if (kpv.Value.Count > 0) {
                    stringBuilder.Length -= 2; // Remove trailing comma and space
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEnd();
        }

        // Method to get all links
        public HashSet<MetroStationLink<T>> GetAllLinks() {
            return stationLinks;
        }
    }
}