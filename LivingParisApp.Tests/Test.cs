using System;
using System.Collections.Generic;
using Xunit;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;
using LivingParisApp.Core.Engines.ShortestPaths;
using System.Diagnostics.Metrics;

#region cette partie est pour le premier rendu, rien ne sera reutilise
public class GraphTests {
    /*
    [Fact]
    public void Graph_ShouldInitialize_EmptyGraph() {
        var graph = new Graph<int>();
        Assert.Empty(graph.AdjacencyList);
    }

    [Fact]
    public void Graph_ShouldInitialize_WithLinks() {
        var nodeA = new Node<int>(1);
        var nodeB = new Node<int>(2);
        var link = new Link<int>(nodeA, nodeB, Direction.Undirected, 1.0);

        var graph = new Graph<int>(new List<Link<int>> { link });

        Assert.NotEmpty(graph.AdjacencyList);
        Assert.Contains(nodeA, graph.AdjacencyList.Keys);
        Assert.Contains(nodeB, graph.AdjacencyList.Keys);
    }

    [Fact]
    public void AddEdgeList_Should_Add_UndirectedEdge() {
        var graph = new Graph<int>();
        var nodeA = new Node<int>(1);
        var nodeB = new Node<int>(2);
        var link = new Link<int>(nodeA, nodeB, Direction.Undirected, 2.0);

        graph.AddEdgeList(link);

        Assert.Contains(nodeA, graph.AdjacencyList.Keys);
        Assert.Contains(nodeB, graph.AdjacencyList.Keys);
        Assert.Contains(Tuple.Create(nodeB, 2.0), graph.AdjacencyList[nodeA]);
        Assert.Contains(Tuple.Create(nodeA, 2.0), graph.AdjacencyList[nodeB]);
    }

    [Fact]
    public void AddEdgeMatrix_Should_Add_WeightedEdge() {
        var nodeA = new Node<int>(1);
        var nodeB = new Node<int>(2);
        var link = new Link<int>(nodeA, nodeB, Direction.Direct, 5.0);
        var graph = new Graph<int>(new List<Link<int>> { link });

        int indexA = graph.nodeToIndexMap[nodeA];
        int indexB = graph.nodeToIndexMap[nodeB];

        Assert.Equal(5, graph.AdjacencyMatrix[indexA, indexB]);
    }

    [Fact]
    public void DisplayAdjacencyMatrix_Should_Return_CorrectFormat() {
        var nodeA = new Node<int>(1);
        var nodeB = new Node<int>(2);
        var link = new Link<int>(nodeA, nodeB, Direction.Direct, 1.0);
        var graph = new Graph<int>(new List<Link<int>> { link });

        string matrixDisplay = graph.DisplayAdjacencyMatrix();
        Assert.Contains("1", matrixDisplay);
    }
    */
}
#endregion

public class ShortestPathTests {
    #region Dijkstra Test
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
        map.AddEdge(stationA.Object, stationB.Object, 4.0);
        map.AddEdge(stationA.Object, stationC.Object, 2.0);
        map.AddEdge(stationB.Object, stationD.Object, 3.0);
        map.AddEdge(stationC.Object, stationD.Object, 5.0);

        // Initialize Dijkstra
        var dijkstra = new Dijkstra<MetroStation>();
        dijkstra.Init(map);

        // Act
        var (path, totalLength) = dijkstra.GetPath(stationA, stationD);

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
    #endregion

    #region Bellman-Ford Test
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
        map.AddEdge(stationA.Object, stationB.Object, 4.0);
        map.AddEdge(stationA.Object, stationC.Object, 2.0);
        map.AddEdge(stationB.Object, stationD.Object, 3.0);
        map.AddEdge(stationC.Object, stationD.Object, 5.0);

        // Initialize Bellman-Ford
        var bellmanFord = new BellmanFord<MetroStation>();
        bellmanFord.Init(map);

        // Act
        var (path, totalLength) = bellmanFord.GetPath(stationA, stationD);

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
    #endregion

    #region Floyd-Warshall Test
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
    #endregion
}   