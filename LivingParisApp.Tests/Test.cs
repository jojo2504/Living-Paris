using System;
using System.Collections.Generic;
using Xunit;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;
using LivingParisApp.Core.Engines.ShortestPaths;
using System.Diagnostics.Metrics;
using LivingParisApp.Core.Engines.GraphColoration;
using Xunit.Abstractions;

public class AdditionalShortestPathTests {
    [Fact]
    public void Dijkstra_EmptyPath_WhenNoPathExists() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003"));

        map.AddEdge(stationA.Object, stationB.Object, 10.0);
        map.AddEdge(stationA.Object, stationC.Object, double.PositiveInfinity);

        var dijkstra = new Dijkstra<MetroStation>();
        dijkstra.Init(map, stationA);

        var (path, totalLength) = dijkstra.GetPath(stationC);

        Assert.Empty(path);
        Assert.Equal(double.PositiveInfinity, totalLength);
    }

    [Fact]
    public void BellmanFord_NegativeWeightEdges_ReturnsShortestPath() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003"));

        map.AddEdge(stationA.Object, stationB.Object, 2.0);
        map.AddEdge(stationB.Object, stationC.Object, -1.0);
        map.AddEdge(stationA.Object, stationC.Object, 3.0);

        var bellmanFord = new BellmanFord<MetroStation>();
        bellmanFord.Init(map, stationA);

        var (path, totalLength) = bellmanFord.GetPath(stationC);

        var expectedPath = new LinkedList<Node<MetroStation>>(
            new[] { stationA, stationB, stationC }
        );

        Assert.Equal(1.0, totalLength);
        Assert.Equal(expectedPath.Count, path.Count);
    }

    [Fact]
    public void FloydWarshall_ComplexGraph_FindsCorrectPaths() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L2", "C", 2, 2, "Paris", "75003"));
        var stationD = new Node<MetroStation>(new MetroStation(4, "L2", "D", 3, 3, "Paris", "75004"));
        var stationE = new Node<MetroStation>(new MetroStation(5, "L3", "E", 4, 4, "Paris", "75005"));

        map.AddEdge(stationA.Object, stationB.Object, 2.0);
        map.AddEdge(stationB.Object, stationC.Object, 3.0);
        map.AddEdge(stationC.Object, stationD.Object, 1.0);
        map.AddEdge(stationD.Object, stationE.Object, 2.0);
        map.AddEdge(stationA.Object, stationE.Object, 10.0);

        var floydWarshall = new FloydWarshall<MetroStation>();
        floydWarshall.Init(map);

        var (path, totalLength) = floydWarshall.GetPath(stationA, stationE);

        var expectedPath = new LinkedList<Node<MetroStation>>(
            new[] { stationA, stationB, stationC, stationD, stationE }
        );

        Assert.Equal(8.0, totalLength);
        Assert.Equal(expectedPath.Count, path.Count);
    }

    [Fact]
    public void Astar_MultiplePossiblePaths_FindsOptimalPath() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 48.8566, 2.3522, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 48.8570, 2.3622, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 48.8600, 2.3700, "Paris", "75003"));
        var stationD = new Node<MetroStation>(new MetroStation(4, "L1", "D", 48.8650, 2.3800, "Paris", "75004"));
        var stationE = new Node<MetroStation>(new MetroStation(5, "L1", "E", 48.8700, 2.3900, "Paris", "75005"));

        map.AddEdge(stationA.Object, stationB.Object, 1.0);
        map.AddEdge(stationB.Object, stationC.Object, 2.0);
        map.AddEdge(stationC.Object, stationE.Object, 3.0);
        map.AddEdge(stationA.Object, stationD.Object, 4.0);
        map.AddEdge(stationD.Object, stationE.Object, 2.0);

        var aStar = new Astar<MetroStation>();
        var (path, totalLength) = aStar.Run(map, stationA, stationE);

        Assert.NotNull(path);
        Assert.NotEmpty(path);

        // Check that we have either path A-B-C-E (6.0) or A-D-E (6.0)
        if (path.Count == 4) {
            Assert.Equal(6.0, totalLength);
            Assert.Equal("A", path.First.Value.Object.LibelleStation);
            Assert.Equal("E", path.Last.Value.Object.LibelleStation);
        }
        else if (path.Count == 3) {
            Assert.Equal(6.0, totalLength);
            Assert.Equal("A", path.First.Value.Object.LibelleStation);
            Assert.Equal("E", path.Last.Value.Object.LibelleStation);
        }
        else {
            Assert.Fail("Path should be either 3 or 4 nodes long");
        }
    }

    [Fact]
    public void Dijkstra_SingleVertexPath_ReturnsZeroLengthPath() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));

        map.AddEdge(stationA.Object, stationB.Object, 5.0);

        var dijkstra = new Dijkstra<MetroStation>();
        dijkstra.Init(map, stationA);

        var (path, totalLength) = dijkstra.GetPath(stationA);

        Assert.NotNull(path);
        Assert.Single(path);
        Assert.Equal(0.0, totalLength);
        Assert.Equal(stationA, path.First());
    }

    [Fact]
    public void BellmanFord_DetectsNegativeCycle() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003"));

        map.AddEdge(stationA.Object, stationB.Object, 1.0);
        map.AddEdge(stationB.Object, stationC.Object, 2.0);
        map.AddEdge(stationC.Object, stationA.Object, -4.0);

        var bellmanFord = new BellmanFord<MetroStation>();

        Assert.Throws<InvalidOperationException>(() => bellmanFord.Init(map, stationA));
    }

    [Fact]
    public void FloydWarshall_TransitivePathFinding() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003"));

        map.AddEdge(stationA.Object, stationB.Object, 1.0);
        map.AddEdge(stationB.Object, stationC.Object, 2.0);

        var floydWarshall = new FloydWarshall<MetroStation>();
        floydWarshall.Init(map);

        var (path, totalLength) = floydWarshall.GetPath(stationA, stationC);

        var expectedPath = new LinkedList<Node<MetroStation>>(
            new[] { stationA, stationB, stationC }
        );

        Assert.Equal(3.0, totalLength);
        Assert.Equal(expectedPath.Count, path.Count);
    }

    [Fact]
    public void Astar_PerformanceComparedToDijkstra() {
        var map = new Map<MetroStation>();

        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 48.8566, 2.3522, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 48.8570, 2.3622, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 48.8600, 2.3700, "Paris", "75003"));
        var stationD = new Node<MetroStation>(new MetroStation(4, "L1", "D", 48.8650, 2.3800, "Paris", "75004"));

        double abDistance = stationA.Object.CalculateDistance(stationB.Object);
        double bcDistance = stationB.Object.CalculateDistance(stationC.Object);
        double cdDistance = stationC.Object.CalculateDistance(stationD.Object);

        map.AddEdge(stationA.Object, stationB.Object, abDistance);
        map.AddEdge(stationB.Object, stationC.Object, bcDistance);
        map.AddEdge(stationC.Object, stationD.Object, cdDistance);

        var aStar = new Astar<MetroStation>();
        var (aStarPath, aStarLength) = aStar.Run(map, stationA, stationD);

        var dijkstra = new Dijkstra<MetroStation>();
        dijkstra.Init(map, stationA);
        var (dijkstraPath, dijkstraLength) = dijkstra.GetPath(stationD);

        Assert.Equal(dijkstraLength, aStarLength, 6);
        Assert.Equal(dijkstraPath.Count, aStarPath.Count);
    }

    [Fact]
    public void Dijkstra_GetPath_ReturnsShortestPath() {
        // Arrange: Create a small test graph
        var map = new Map<MetroStation>();

        // Create stations wrapped in Node<T> as per original Dijkstra test
        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003"));
        var stationD = new Node<MetroStation>(new MetroStation(4, "L1", "D", 3, 3, "Paris", "75004"));

        // Set up the graph exactly as in Dijkstra test
        map.AddEdge(stationA.Object, stationB.Object, 10.0);
        map.AddEdge(stationA.Object, stationC.Object, 2.0);
        map.AddEdge(stationB.Object, stationD.Object, 3.0);
        map.AddEdge(stationC.Object, stationD.Object, 5.0);

        // Initialize Dijkstra
        var dijkstra = new Dijkstra<MetroStation>();
        dijkstra.Init(map, stationA);

        // Act
        var (path, totalLength) = dijkstra.GetPath(stationD);

        // Assert
        var expectedPath = new LinkedList<Node<MetroStation>>(
            new[] { stationA, stationC, stationD }
        );

        Assert.NotNull(path);
        Assert.Equal(7.0, totalLength);
        Assert.Equal(expectedPath.Count, path.Count);

        var pathList = path.ToList();
        var expectedList = expectedPath.ToList();
        for (int i = 0; i < expectedPath.Count; i++) {
            Assert.Equal(expectedList[i].Object.LibelleStation, pathList[i].Object.LibelleStation);
        }
    }

    [Fact]
    public void BellmanFord_GetPath_ReturnsShortestPath() {
        // Arrange: Create a small test graph
        var map = new Map<MetroStation>();

        // Create stations wrapped in Node<T> as per original Dijkstra test
        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003"));
        var stationD = new Node<MetroStation>(new MetroStation(4, "L1", "D", 3, 3, "Paris", "75004"));

        // Set up the graph exactly as in Dijkstra test
        map.AddEdge(stationA.Object, stationB.Object, 10.0);
        map.AddEdge(stationA.Object, stationC.Object, 2.0);
        map.AddEdge(stationB.Object, stationD.Object, 3.0);
        map.AddEdge(stationC.Object, stationD.Object, 5.0);

        // Initialize Bellman-Ford
        var bellmanFord = new BellmanFord<MetroStation>();
        bellmanFord.Init(map, stationA);

        // Act
        var (path, totalLength) = bellmanFord.GetPath(stationD);

        // Assert
        var expectedPath = new LinkedList<Node<MetroStation>>(
            new[] { stationA, stationC, stationD }
        );

        Assert.NotNull(path);
        Assert.Equal(7.0, totalLength);
        Assert.Equal(expectedPath.Count, path.Count);

        var pathList = path.ToList();
        var expectedList = expectedPath.ToList();
        for (int i = 0; i < expectedPath.Count; i++) {
            Assert.Equal(expectedList[i].Object.LibelleStation, pathList[i].Object.LibelleStation);
        }
    }

    [Fact]
    public void FloydWarshall_GetPath_ReturnsShortestPath() {
        // Arrange: Create a small test graph
        var map = new Map<MetroStation>();

        // Create stations wrapped in Node<T> as per original Dijkstra test
        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001"));
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002"));
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003"));
        var stationD = new Node<MetroStation>(new MetroStation(4, "L1", "D", 3, 3, "Paris", "75004"));

        // Set up the graph exactly as in Dijkstra test
        map.AddEdge(stationA.Object, stationB.Object, 4.0);
        map.AddEdge(stationA.Object, stationC.Object, 2.0);
        map.AddEdge(stationB.Object, stationD.Object, 3.0);
        map.AddEdge(stationC.Object, stationD.Object, 10.0);

        // Initialize Floyd-Warshall
        var floydWarshall = new FloydWarshall<MetroStation>();
        floydWarshall.Init(map);

        // Act
        var (path, totalLength) = floydWarshall.GetPath(stationA, stationD);

        // Assert
        var expectedPath = new LinkedList<Node<MetroStation>>(
            new[] { stationA, stationB, stationD }
        );

        Assert.NotNull(path);
        Assert.Equal(7.0, totalLength);
        Assert.Equal(expectedPath.Count, path.Count);

        var pathList = path.ToList();
        var expectedList = expectedPath.ToList();
        for (int i = 0; i < expectedPath.Count; i++) {
            Assert.Equal(expectedList[i].Object.LibelleStation, pathList[i].Object.LibelleStation);
        }
    }

    [Fact]
    public void Astar_GetPath_ReturnsShortestPath() {
        // Arrange: Create a test graph with realistic distances
        var map = new Map<MetroStation>();

        // Create stations wrapped in Node<T>
        var stationA = new Node<MetroStation>(new MetroStation(1, "L1", "A", 48.8566, 2.3522, "Paris", "75001")); // Paris
        var stationB = new Node<MetroStation>(new MetroStation(2, "L1", "B", 48.8570, 2.3622, "Paris", "75002")); // Close to A
        var stationC = new Node<MetroStation>(new MetroStation(3, "L1", "C", 48.8600, 2.3700, "Paris", "75003")); // Further away
        var stationD = new Node<MetroStation>(new MetroStation(4, "L1", "D", 48.8650, 2.3800, "Paris", "75004")); // Destination

        // Compute weights using Haversine formula
        double abDistance = stationA.Object.CalculateDistance(stationB.Object);
        double acDistance = stationA.Object.CalculateDistance(stationC.Object);
        double bdDistance = stationB.Object.CalculateDistance(stationD.Object);
        double cdDistance = stationC.Object.CalculateDistance(stationD.Object);

        // Set up the graph with distances
        map.AddEdge(stationA.Object, stationB.Object, abDistance);
        map.AddEdge(stationA.Object, stationC.Object, acDistance);
        map.AddEdge(stationB.Object, stationD.Object, bdDistance);
        map.AddEdge(stationC.Object, stationD.Object, cdDistance);

        // Initialize A* Algorithm
        var aStar = new Astar<MetroStation>();
        var (path, totalLength) = aStar.Run(map, stationA, stationD);

        // Assert
        Assert.NotNull(path);
        Assert.NotEmpty(path);
        Assert.True(totalLength > 0, "Total length should be a positive value");

        // Extract station names from the path
        var pathStations = path.Select(node => node.Object.LibelleStation).ToList();

        // Since A* is heuristic-based, multiple shortest paths might exist
        var possiblePaths = new List<List<string>> {
        new() { "A", "B", "D" },
        new() { "A", "C", "D" }
    };

        Assert.Contains(pathStations, possiblePaths);
    }
}

public class WelshPowellTests {
    private readonly ITestOutputHelper _output;

    public WelshPowellTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void ColorGraph_SimpleLinearGraph_ShouldRequireExactlyTwoColors() {
        // Arrangement
        var map = new Map<MetroStation>();

        var stationA = new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001");
        var stationB = new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002");
        var stationC = new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003");
        var stationD = new MetroStation(4, "L2", "D", 3, 3, "Paris", "75004");
        var stationE = new MetroStation(5, "L2", "E", 4, 4, "Paris", "75005");

        // Create graph structure
        map.AddBidirectionalEdge(stationA, stationB, 1.0);
        map.AddBidirectionalEdge(stationB, stationC, 1.0);
        map.AddBidirectionalEdge(stationC, stationD, 1.0);
        map.AddBidirectionalEdge(stationD, stationE, 1.0);

        _output.WriteLine(map.ToString());

        var welshPowell = new WelshPowell<MetroStation>(map);

        // Action
        var colorAssignment = welshPowell.ColorGraph();
        var colorCount = welshPowell.GetColorCount();

        _output.WriteLine($"{colorCount}");
        
        // Assert
        Assert.Equal(2, colorCount);

        // Group nodes by their station ID for easier testing
        var nodesByStationId = new Dictionary<int, Node<MetroStation>>();
        foreach (var node in colorAssignment.Keys) {
            nodesByStationId[node.Object.ID] = node;
        }

        // Now we can use station IDs to access the correct nodes
        Assert.NotEqual(colorAssignment[nodesByStationId[1]], colorAssignment[nodesByStationId[2]]);
        Assert.Equal(colorAssignment[nodesByStationId[1]], colorAssignment[nodesByStationId[3]]);
        Assert.Equal(colorAssignment[nodesByStationId[2]], colorAssignment[nodesByStationId[4]]);
        Assert.Equal(colorAssignment[nodesByStationId[3]], colorAssignment[nodesByStationId[5]]);
    }

    [Fact]
    public void ColorGraph_CompleteGraph_ShouldRequireNumberOfNodesColors() {
        // Arrangement
        var map = new Map<MetroStation>();

        var stationA = new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001");
        var stationB = new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002");
        var stationC = new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003");
        var stationD = new MetroStation(4, "L2", "D", 3, 3, "Paris", "75004");

        // Create complete graph (every node connected to every other node)
        map.AddBidirectionalEdge(stationA, stationB, 1.0);
        map.AddBidirectionalEdge(stationA, stationC, 1.0);
        map.AddBidirectionalEdge(stationA, stationD, 1.0);
        map.AddBidirectionalEdge(stationB, stationC, 1.0);
        map.AddBidirectionalEdge(stationB, stationD, 1.0);
        map.AddBidirectionalEdge(stationC, stationD, 1.0);

        var welshPowell = new WelshPowell<MetroStation>(map);

        // Action
        var colorAssignment = welshPowell.ColorGraph();
        var colorCount = welshPowell.GetColorCount();

        // Assert
        Assert.Equal(4, colorCount); // Complete graph with 4 nodes needs 4 colors

        // Verify no adjacent nodes have the same color
        foreach (var node in map.AdjacencyList.Keys) {
            foreach (var adjacentNodeTuple in map.AdjacencyList[node]) {
                var adjacentNode = adjacentNodeTuple.Item1;
                Assert.NotEqual(colorAssignment[node], colorAssignment[adjacentNode]);
            }
        }
    }

    [Fact]
    public void ColorGraph_CycleGraph_ShouldRequireExactlyThreeColors() {
        // Arrangement
        var map = new Map<MetroStation>();

        var stationA = new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001");
        var stationB = new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002");
        var stationC = new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003");
        var stationD = new MetroStation(4, "L2", "D", 3, 3, "Paris", "75004");
        var stationE = new MetroStation(5, "L2", "E", 4, 4, "Paris", "75005");

        // Create a cycle graph
        map.AddBidirectionalEdge(stationA, stationB, 1.0);
        map.AddBidirectionalEdge(stationB, stationC, 1.0);
        map.AddBidirectionalEdge(stationC, stationD, 1.0);
        map.AddBidirectionalEdge(stationD, stationE, 1.0);
        map.AddBidirectionalEdge(stationE, stationA, 1.0); // Close the cycle

        var welshPowell = new WelshPowell<MetroStation>(map);

        // Action
        var colorAssignment = welshPowell.ColorGraph();
        var colorCount = welshPowell.GetColorCount();

        // Assert
        Assert.True(colorCount <= 3, $"Expected at most 3 colors, but got {colorCount}");

        // Verify no adjacent nodes have the same color
        foreach (var node in map.AdjacencyList.Keys) {
            foreach (var adjacentNodeTuple in map.AdjacencyList[node]) {
                var adjacentNode = adjacentNodeTuple.Item1;
                Assert.NotEqual(colorAssignment[node], colorAssignment[adjacentNode]);
            }
        }
    }

    [Fact]
    public void GetNodesGroupedByColor_ShouldGroupCorrectly() {
        // Arrangement
        var map = new Map<MetroStation>();

        var stationA = new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001");
        var stationB = new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002");
        var stationC = new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003");
        var stationD = new MetroStation(4, "L2", "D", 3, 3, "Paris", "75004");
        var stationE = new MetroStation(5, "L2", "E", 4, 4, "Paris", "75005");

        // Create graph structure
        map.AddBidirectionalEdge(stationA, stationB, 1.0);
        map.AddBidirectionalEdge(stationA, stationC, 1.0);
        map.AddBidirectionalEdge(stationB, stationD, 1.0);
        map.AddBidirectionalEdge(stationC, stationD, 1.0);
        map.AddBidirectionalEdge(stationD, stationE, 1.0);

        var welshPowell = new WelshPowell<MetroStation>(map);

        // Action
        var colorAssignment = welshPowell.ColorGraph();
        var nodesGroupedByColor = welshPowell.GetNodesGroupedByColor();

        // Assert
        foreach (var colorGroup in nodesGroupedByColor) {
            var color = colorGroup.Key;
            var nodes = colorGroup.Value;

            // All nodes in a group should have the same color
            foreach (var node in nodes) {
                Assert.Equal(color, colorAssignment[node]);
            }

            // No adjacent nodes should be in the same group
            for (int i = 0; i < nodes.Count; i++) {
                for (int j = i + 1; j < nodes.Count; j++) {
                    var node1 = nodes[i];
                    var node2 = nodes[j];

                    // Check they're not adjacent
                    if (map.AdjacencyList.ContainsKey(node1) &&
                        map.AdjacencyList[node1].Any(t => t.Item1 == node2)) {
                        Assert.Fail($"Adjacent nodes {node1.Object.LibelleStation} and {node2.Object.LibelleStation} have the same color {color}");
                    }
                }
            }
        }
    }

    [Fact]
    public void Reset_ShouldClearColorAssignments() {
        // Arrangement
        var map = new Map<MetroStation>();

        var stationA = new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001");
        var stationB = new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002");
        var stationC = new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003");

        map.AddBidirectionalEdge(stationA, stationB, 1.0);
        map.AddBidirectionalEdge(stationB, stationC, 1.0);

        var welshPowell = new WelshPowell<MetroStation>(map);

        // Action 1: Color the graph
        var colorAssignment1 = welshPowell.ColorGraph();

        // Action 2: Reset coloring
        welshPowell.Reset();

        // Action 3: Color again
        var colorAssignment2 = welshPowell.ColorGraph();

        // Assert
        Assert.NotSame(colorAssignment1, colorAssignment2); // They should be different object instances
        Assert.Equal(colorAssignment1.Count, colorAssignment2.Count); // But have the same number of nodes
    }
    
    [Fact]
    public void IsBipartite_ShouldReturnTrue() {
        // Arrangement
        var map = new Map<MetroStation>();

        var stationA = new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001");
        var stationB = new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002");
        var stationC = new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003");

        map.AddBidirectionalEdge(stationA, stationB, 1.0);
        map.AddBidirectionalEdge(stationB, stationC, 1.0);

        var welshPowell = new WelshPowell<MetroStation>(map);

        Assert.True(welshPowell.IsBipartite());
    }

    [Fact]
    public void IsBipartite_ShouldReturnFalse() {
        // Arrangement
        var map = new Map<MetroStation>();

        var stationA = new MetroStation(1, "L1", "A", 0, 0, "Paris", "75001");
        var stationB = new MetroStation(2, "L1", "B", 1, 1, "Paris", "75002");
        var stationC = new MetroStation(3, "L1", "C", 2, 2, "Paris", "75003");
        var stationD = new MetroStation(4, "L2", "D", 3, 3, "Paris", "75004");

        // Create complete graph (every node connected to every other node)
        map.AddBidirectionalEdge(stationA, stationB, 1.0);
        map.AddBidirectionalEdge(stationA, stationC, 1.0);
        map.AddBidirectionalEdge(stationA, stationD, 1.0);
        map.AddBidirectionalEdge(stationB, stationC, 1.0);
        map.AddBidirectionalEdge(stationB, stationD, 1.0);
        map.AddBidirectionalEdge(stationC, stationD, 1.0);

        var welshPowell = new WelshPowell<MetroStation>(map);

        Assert.False(welshPowell.IsBipartite());
    }
}