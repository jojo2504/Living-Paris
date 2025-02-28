using System;
using System.Collections.Generic;
using Xunit;
using LivingParisApp.Core.GraphStructure;

public class GraphTests {
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
}