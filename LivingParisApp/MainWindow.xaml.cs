using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using LivingParisApp.Core.GraphStructure;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using LivingParisApp.Services.Logging;

/*
I want you to create a C# WPF application called LivingParisApp that visualizes and interacts with an undirected graph structure. The application should display nodes and edges on a canvas, implement graph traversal algorithms (BFS and DFS), and include physics-based node movement with toggleable force modes. Below are the detailed requirements for the project. Please implement this with proper error handling, logging, and a clean object-oriented design.

Project Overview
Namespace: LivingParisApp
Main Window: A WPF MainWindow class that serves as the entry point and hosts a Canvas (named GraphCanvas) for rendering the graph.
Graph Representation: Use a generic Graph<T> class (assumed to be predefined in LivingParisApp.Core.GraphStructure) with an adjacency list structure. For this implementation, assume T is int by default, and the graph is undirected and unweighted unless specified otherwise.
Visualization: Nodes should be represented as ellipses with labels, and edges as lines connecting them. The layout should be dynamic, with nodes initially placed randomly and adjusted via physics simulation or user dragging.
Core Components
MainWindow Class
Inherits from System.Windows.Window.
Takes an optional Graph<int> parameter in the constructor (defaults to a new graph if none provided).
Initializes a GraphVisualizer<int> instance to handle rendering and interaction.
Defines event handlers for toggling search mode, starting searches, and toggling physics mode, delegating to the visualizer.
IGraphVisualizer Interface
Defines methods: ToggleSearchMode, StartSearch, and TogglePhysicsMode to standardize the visualizer’s public API.
GraphVisualizer<T> Class
Implements IGraphVisualizer.
Manages the visualization and interaction of the graph on the provided Canvas.
Generic type T matches the graph’s node type.
Visualization Requirements
Nodes:
Represented as Ellipse objects with a TextBlock label showing the node’s value (node.Object.ToString()).
Size varies based on the number of connections (e.g., base size of 40 + 5 per connection).
Positioned initially at random coordinates within the canvas bounds (50 to canvasWidth-50, 50 to canvasHeight-50).
Stored in dictionaries for positions (Point), shapes (Ellipse), labels (TextBlock), and velocities (Vector).
Edges:
Drawn as Line objects connecting node centers.
Stored in dictionaries for lines per node and edge states (with a tuple of Line and state string like "Default", "Current", "Explored", or "Path").
Undirected edges should only be drawn once (avoid duplicates).
User Interaction
Dragging:
Nodes can be dragged by left-clicking and moving the mouse (either on the ellipse or its text).
Update node positions and connected edges in real-time.
Node Selection:
Left-click: Display node stats (name, connections, graph order, size, type, connectivity, cycle presence).
Right-click: Set/unset the node as the target (highlighted with a red border).
Double-click: Start BFS/DFS from that node.
UI Controls:
Buttons: "Switch to DFS" (toggles BFS/DFS), "Play" (starts search), "Toggle Forces" (switches physics mode), "Reset" (clears search state).
TextBlocks: Show current search mode, target status, instructions, node stats, and physics mode.
Physics Simulation
Timer: Use a DispatcherTimer with a 16ms interval (~60 FPS) for physics updates.
Modes:
Magnetic Mode (default):
Repulsion: Nodes push each other away with force proportional to MagneticRepulsionConstant / distance^2, decaying exponentially beyond 300 units.
Attraction: Connected nodes pull toward each other with a spring force (MagneticSpringConstant * (distance - 100)).
Repulsion Mode:
Repulsion only: Stronger force (RepulsionConstant / distance^2) without attraction.
Dragging Repulsion: Extra force (DraggingRepulsionConstant) when dragging a node, scaled by its connections.
Constants:
MagneticSpringConstant = 0.01, MagneticRepulsionConstant = 1000.0, RepulsionConstant = 500.0, DraggingRepulsionConstant = 2000.0.
Damping = 0.85, MaxVelocity = 50.0, MinDistance = 10.0, RepulsionDecayRate = 0.01.
Boundary Handling: Keep nodes within canvas bounds, accounting for their size.
Search Algorithms
Timer: Use a DispatcherTimer with a 1000ms interval for step-by-step visualization.
BFS:
Process one layer per tick, enqueueing unvisited neighbors.
Highlight the current layer, dim previous nodes.
DFS:
Process one node per tick, pushing unvisited neighbors in reverse order.
Highlight the current node, dim others.
Visual Feedback:
Nodes: Highlight current (yellow, scaling animation), dim explored (gray-blue), reset to default (dark blue).
Edges: Animate "Current" (yellow, fading in), set "Explored" (gray), reset to "Default" (gray).
Target: Green fill when found, with the shortest path highlighted in yellow.
Path Display: On completion, show the path to the target (if set) or all explored paths in nodeStatsText.
Graph Analysis
Connectedness: Check if all nodes are reachable from one via BFS.
Cycles: Detect using DFS with a recursion stack.
Order: Number of nodes.
Size: Number of unique edges.
Type: Report as "Undirected, Unweighted" (hardcode for simplicity).
Logging
Use a Logger class (assumed in LivingParisApp.Services.Logging) to log initialization, errors, and key events (e.g., search start/completion, physics toggle).
Error Handling
Wrap all methods in try-catch blocks, logging exceptions via Logger.Log(ex).
Handle edge cases like empty graphs, null references, or invalid canvas sizes.
Implementation Notes
Use WPF animations for smooth transitions (e.g., edge fading, node scaling).
Ensure thread safety with DispatcherTimer.
Keep the code modular, with separate methods for drawing, physics, search, and UI setup.
Please generate the complete C# code for this application, including all classes and methods described. The result should be a functional WPF app ready to visualize and interact with a graph.
*/

namespace LivingParisApp {
    public partial class MainWindow : Window {
        private readonly IGraphVisualizer graphVisualizer;

        public MainWindow()
            : this(new Graph<int>(s => int.Parse(s), null)) // Default to Graph<int> for soc-karate.mtx
        {
        }

        public MainWindow(Graph<int> graph) {
            InitializeComponent();
            graphVisualizer = new GraphVisualizer<int>(graph, GraphCanvas);
            Logger.Log("MainWindow initialized with Graph<int>");
        }

        private void ToggleSearchMode(object sender, RoutedEventArgs e) => graphVisualizer.ToggleSearchMode(sender, e);
        private void StartSearch(object sender, RoutedEventArgs e) => graphVisualizer.StartSearch(sender, e);
        private void TogglePhysicsMode(object sender, RoutedEventArgs e) => graphVisualizer.TogglePhysicsMode(sender, e);
    }

    public interface IGraphVisualizer {
        void ToggleSearchMode(object sender, RoutedEventArgs e);
        void StartSearch(object sender, RoutedEventArgs e);
        void TogglePhysicsMode(object sender, RoutedEventArgs e);
    }

    public class GraphVisualizer<T> : IGraphVisualizer {
        private readonly Graph<T> graph;
        private readonly Canvas graphCanvas;
        private readonly Dictionary<Node<T>, Point> nodePositions = new();
        private readonly Dictionary<Node<T>, Ellipse> nodeShapes = new();
        private readonly Dictionary<Node<T>, TextBlock> nodeLabels = new();
        private readonly Dictionary<Node<T>, List<Line>> edgeLines = new();
        private readonly Dictionary<(Node<T>, Node<T>), (Line Line, string State)> edgeStates = new();
        private readonly Dictionary<Node<T>, Node<T>> parentNodes = new();
        private readonly Dictionary<Node<T>, Vector> nodeVelocities = new();

        private Node<T> startNode;
        private Node<T> draggingNode;
        private Point lastMousePosition;

        private Queue<Node<T>> bfsQueue;
        private Stack<Node<T>> dfsStack;
        private DispatcherTimer searchTimer;
        private DispatcherTimer physicsTimer;
        private HashSet<Node<T>> visitedNodes;
        private readonly HashSet<Node<T>> animatedNodes = new();
        private bool isBFS = true;
        private Node<T> targetNode;
        private bool targetFound = false;
        private TextBlock searchModeText;
        private TextBlock targetStatusText;
        private TextBlock instructionsText;
        private TextBlock nodeStatsText;
        private TextBlock physicsModeText;
        private bool isMagneticMode = true;

        private const double MagneticSpringConstant = 0.01;
        private const double MagneticRepulsionConstant = 1000.0;
        private const double RepulsionConstant = 500.0;
        private const double DraggingRepulsionConstant = 2000.0;
        private const double Damping = 0.85;
        private const double MaxVelocity = 50.0;
        private const double MinDistance = 10.0;
        private const double RepulsionDecayRate = 0.01;

        public GraphVisualizer(Graph<T> graph, Canvas canvas) {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            this.graphCanvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            graphCanvas.Loaded += (s, e) => InitializeGraph();
        }

        private void InitializeGraph() {
            try {
                Logger.Log("Initializing graph visualization");
                DrawGraph();
                SetupUIControls();

                searchTimer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(1000)
                };
                searchTimer.Tick += OnSearchTick;

                physicsTimer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                physicsTimer.Tick += OnPhysicsTick;
                physicsTimer.Start();

                Logger.Log("Graph visualization initialized");
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void DrawGraph() {
            try {
                Logger.Log("DrawGraph started");
                Random rand = new Random();
                double canvasWidth = graphCanvas.ActualWidth;
                double canvasHeight = graphCanvas.ActualHeight;

                if (canvasWidth <= 0 || canvasHeight <= 0) {
                    Logger.Log("Canvas size not yet available, skipping node placement");
                    return;
                }

                foreach (var node in graph.AdjacencyList.Keys) {
                    if (!nodePositions.ContainsKey(node)) {
                        int minX = 50;
                        int maxX = (int)canvasWidth - 50;
                        int minY = 50;
                        int maxY = (int)canvasHeight - 50;

                        nodePositions[node] = new Point(rand.Next(minX, maxX), rand.Next(minY, maxY));
                        nodeVelocities[node] = new Vector(0, 0);
                    }
                }

                HashSet<(Node<T>, Node<T>)> drawnEdges = new();
                foreach (var kvp in graph.AdjacencyList) {
                    var fromNode = kvp.Key;
                    foreach (var edge in kvp.Value) {
                        var toNode = edge.Item1;
                        if (!drawnEdges.Contains((fromNode, toNode)) && !drawnEdges.Contains((toNode, fromNode))) {
                            DrawEdge(fromNode, toNode);
                            drawnEdges.Add((fromNode, toNode));
                        }
                    }
                }

                foreach (var node in graph.AdjacencyList.Keys) {
                    DrawNode(node);
                }
                Logger.Log($"DrawGraph completed with {nodePositions.Count} nodes and {edgeStates.Count} edges");
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void SetupUIControls() {
            try {
                Logger.Log("SetupUIControls started");
                Button toggleButton = new Button {
                    Content = "Switch to DFS",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(10, 10, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                toggleButton.Click += ToggleSearchMode;
                graphCanvas.Children.Add(toggleButton);

                Button playButton = new Button {
                    Content = "Play",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(120, 10, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                playButton.Click += StartSearch;
                graphCanvas.Children.Add(playButton);

                searchModeText = new TextBlock {
                    Text = "Current Mode: BFS",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 16,
                    Margin = new Thickness(230, 15, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                graphCanvas.Children.Add(searchModeText);

                targetStatusText = new TextBlock {
                    Text = "Target: None",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 16,
                    Margin = new Thickness(400, 15, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                graphCanvas.Children.Add(targetStatusText);

                instructionsText = new TextBlock {
                    Text = "Instructions:\n- Click 'Switch to DFS' to toggle BFS/DFS.\n- Click 'Play' to start the search.\n- Right-click a node to set it as the target (red border).\n- Double-click a node to start search from it.\n- Left-click a node to see its stats.\n- Drag nodes (or text) to reposition.\n- Click 'Toggle Forces' to switch physics.",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 14,
                    Margin = new Thickness(10, 50, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 300
                };
                graphCanvas.Children.Add(instructionsText);

                nodeStatsText = new TextBlock {
                    Text = "Node Stats: None",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 14,
                    Margin = new Thickness(320, 50, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 200
                };
                graphCanvas.Children.Add(nodeStatsText);

                Button toggleForcesButton = new Button {
                    Content = "Toggle Forces",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(10, 220, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                toggleForcesButton.Click += TogglePhysicsMode;
                graphCanvas.Children.Add(toggleForcesButton);

                physicsModeText = new TextBlock {
                    Text = "Physics: Magnetic",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 16,
                    Margin = new Thickness(120, 225, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                graphCanvas.Children.Add(physicsModeText);

                Button resetButton = new Button {
                    Content = "Reset",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(230, 220, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                resetButton.Click += ResetSearch;
                graphCanvas.Children.Add(resetButton);

                Logger.Log("SetupUIControls completed");
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void DrawNode(Node<T> node) {
            try {
                Point position = nodePositions[node];
                int connectionCount = graph.AdjacencyList[node].Count;
                double size = 40 + (connectionCount * 5);
                Canvas nodeContainer = new Canvas { Width = size + 20, Height = size + 20 };

                Ellipse ellipse = new Ellipse {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Color.FromRgb(20, 20, 35)),
                    Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 100)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(ellipse, 10);
                Canvas.SetTop(ellipse, 10);

                TextBlock text = new TextBlock {
                    Text = node.Object.ToString(),
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center
                };
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Size textSize = text.DesiredSize;
                Canvas.SetLeft(text, (size - textSize.Width) / 2 + 10);
                Canvas.SetTop(text, (size - textSize.Height) / 2 + 10);

                nodeContainer.Children.Add(ellipse);
                nodeContainer.Children.Add(text);

                Canvas.SetLeft(nodeContainer, position.X - (size + 20) / 2);
                Canvas.SetTop(nodeContainer, position.Y - (size + 20) / 2);

                ellipse.MouseDown += (sender, e) => {
                    if (e.ChangedButton == MouseButton.Left && e.ClickCount >= 2) {
                        StartSearchFromNode(node);
                    }
                    else if (e.ChangedButton == MouseButton.Right) {
                        SetTargetNode(node);
                    }
                    else if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1) {
                        DisplayNodeStats(node);
                    }
                };
                nodeContainer.MouseDown += (sender, e) => StartDragging(node, e);
                nodeContainer.MouseMove += (sender, e) => DragNode(e);
                nodeContainer.MouseUp += (sender, e) => StopDragging();

                text.MouseDown += (sender, e) => StartDragging(node, e);
                text.MouseMove += (sender, e) => DragNode(e);
                text.MouseUp += (sender, e) => StopDragging();

                graphCanvas.Children.Add(nodeContainer);
                nodeShapes[node] = ellipse;
                nodeLabels[node] = text;
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void DrawEdge(Node<T> from, Node<T> to) {
            try {
                Line line = new Line {
                    X1 = nodePositions[from].X,
                    Y1 = nodePositions[from].Y,
                    X2 = nodePositions[to].X,
                    Y2 = nodePositions[to].Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 100)),
                    StrokeThickness = 1,
                    Opacity = 1
                };
                graphCanvas.Children.Add(line);

                var edgeKey = Comparer<T>.Create((a, b) => a.ToString().CompareTo(b.ToString())).Compare(from.Object, to.Object) < 0 ? (from, to) : (to, from);
                edgeStates[edgeKey] = (line, "Default");

                if (!edgeLines.ContainsKey(from)) edgeLines[from] = new List<Line>();
                if (!edgeLines.ContainsKey(to)) edgeLines[to] = new List<Line>();
                edgeLines[from].Add(line);
                edgeLines[to].Add(line);
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void StartDragging(Node<T> node, MouseButtonEventArgs e) {
            try {
                if (node == null) {
                    Logger.Log("Node is null in StartDragging");
                    return;
                }
                draggingNode = node;
                lastMousePosition = e.GetPosition(graphCanvas);
                if (!nodeVelocities.ContainsKey(node)) {
                    Logger.Log($"Initializing velocity for node: {node.Object}");
                    nodeVelocities[node] = new Vector(0, 0);
                }
                else {
                    nodeVelocities[node] = new Vector(0, 0);
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
                draggingNode = default;
            }
        }

        private void DragNode(MouseEventArgs e) {
            try {
                if (Equals(draggingNode, default(Node<T>))) {
                    return;
                }
                Point newMousePosition = e.GetPosition(graphCanvas);
                double dx = newMousePosition.X - lastMousePosition.X;
                double dy = newMousePosition.Y - lastMousePosition.Y;

                if (nodeShapes.TryGetValue(draggingNode, out Ellipse ellipse) && ellipse != null) {
                    Canvas nodeContainer = ellipse.Parent as Canvas;
                    if (nodeContainer != null) {
                        double newX = Canvas.GetLeft(nodeContainer) + dx;
                        double newY = Canvas.GetTop(nodeContainer) + dy;
                        newX = Math.Max(0, Math.Min(newX, graphCanvas.ActualWidth - nodeContainer.Width));
                        newY = Math.Max(0, Math.Min(newY, graphCanvas.ActualHeight - nodeContainer.Height));
                        Canvas.SetLeft(nodeContainer, newX);
                        Canvas.SetTop(nodeContainer, newY);
                        nodePositions[draggingNode] = new Point(newX + nodeContainer.Width / 2, newY + nodeContainer.Height / 2);
                        lastMousePosition = newMousePosition;
                        UpdateAllEdges();
                    }
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void StopDragging() {
            try {
                draggingNode = default;
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void UpdateAllEdges() {
            try {
                foreach (var line in edgeLines.Values.SelectMany(x => x).Distinct()) {
                    var connectedNodes = nodePositions.Keys
                        .Where(n => edgeLines[n].Contains(line))
                        .Take(2).ToList();
                    if (connectedNodes.Count == 2) {
                        line.X1 = nodePositions[connectedNodes[0]].X;
                        line.Y1 = nodePositions[connectedNodes[0]].Y;
                        line.X2 = nodePositions[connectedNodes[1]].X;
                        line.Y2 = nodePositions[connectedNodes[1]].Y;
                    }
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        public void TogglePhysicsMode(object sender, RoutedEventArgs e) {
            try {
                isMagneticMode = !isMagneticMode;
                physicsModeText.Text = isMagneticMode ? "Physics: Magnetic" : "Physics: Repulsion";
                Logger.Log($"Physics mode toggled to: {physicsModeText.Text}");
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void OnPhysicsTick(object sender, EventArgs e) {
            try {
                if (graph.AdjacencyList.Count == 0) return;

                double canvasWidth = Math.Max(graphCanvas.ActualWidth, 200);
                double canvasHeight = Math.Max(graphCanvas.ActualHeight, 200);

                if (canvasWidth <= 0 || canvasHeight <= 0) return;

                foreach (var node1 in graph.AdjacencyList.Keys) {
                    Vector force = new Vector(0, 0);

                    foreach (var node2 in graph.AdjacencyList.Keys) {
                        if (node1 != node2) {
                            Vector diff = nodePositions[node1] - nodePositions[node2];
                            double distance = Math.Max(diff.Length, MinDistance);
                            double repulsion = (isMagneticMode ? MagneticRepulsionConstant : RepulsionConstant) / (distance * distance);
                            if (distance > 300) {
                                repulsion *= Math.Exp(-RepulsionDecayRate * (distance - 300));
                            }
                            force += diff * (repulsion / distance);
                        }
                    }

                    if (isMagneticMode) {
                        foreach (var edge in graph.AdjacencyList[node1]) {
                            Node<T> node2 = edge.Item1;
                            Vector diff = nodePositions[node2] - nodePositions[node1];
                            double distance = diff.Length;
                            double attraction = MagneticSpringConstant * (distance - 100);
                            force += diff * (attraction / Math.Max(distance, MinDistance));
                        }
                    }

                    if (!isMagneticMode && !Equals(draggingNode, default(Node<T>)) && node1 != draggingNode) {
                        Vector diff = nodePositions[node1] - nodePositions[draggingNode];
                        double distance = Math.Max(diff.Length, MinDistance);
                        int draggingConnections = graph.AdjacencyList[draggingNode].Count;
                        double dragForce = DraggingRepulsionConstant * (1 + draggingConnections) / (distance * distance);
                        if (distance < 200) {
                            force += diff * (dragForce / distance);
                        }
                    }

                    if (!Equals(node1, draggingNode)) {
                        if (!nodeVelocities.ContainsKey(node1)) {
                            nodeVelocities[node1] = new Vector(0, 0);
                        }
                        nodeVelocities[node1] += force;
                        if (nodeVelocities[node1].Length > MaxVelocity) {
                            nodeVelocities[node1] = nodeVelocities[node1] * (MaxVelocity / nodeVelocities[node1].Length);
                        }
                        nodeVelocities[node1] *= Damping;

                        Point newPosition = nodePositions[node1] + nodeVelocities[node1];
                        if (!double.IsNaN(newPosition.X) && !double.IsNaN(newPosition.Y)) {
                            double halfWidth = nodeShapes.TryGetValue(node1, out Ellipse ellipse) ? ellipse.Width / 2 : 20;
                            double halfHeight = ellipse != null ? ellipse.Height / 2 : 20;
                            newPosition.X = Math.Max(halfWidth, Math.Min(newPosition.X, canvasWidth - halfWidth));
                            newPosition.Y = Math.Max(halfHeight, Math.Min(newPosition.Y, canvasHeight - halfHeight));
                            nodePositions[node1] = newPosition;

                            if (ellipse != null) {
                                Canvas nodeContainer = ellipse.Parent as Canvas;
                                if (nodeContainer != null) {
                                    Canvas.SetLeft(nodeContainer, newPosition.X - nodeContainer.Width / 2);
                                    Canvas.SetTop(nodeContainer, newPosition.Y - nodeContainer.Height / 2);
                                }
                            }
                        }
                    }
                }

                UpdateAllEdges();
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        public void ToggleSearchMode(object sender, RoutedEventArgs e) {
            try {
                isBFS = !isBFS;
                ((Button)sender).Content = isBFS ? "Switch to DFS" : "Switch to BFS";
                searchModeText.Text = isBFS ? "Current Mode: BFS" : "Current Mode: DFS";
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        public void StartSearch(object sender, RoutedEventArgs e) {
            try {
                if (graph.AdjacencyList.Count == 0) {
                    MessageBox.Show("Graph is empty. Add nodes and edges to start the search.");
                    return;
                }

                // Use the target node if set; otherwise, fall back to the first node
                Node<T> startNode = Equals(targetNode, default(Node<T>))
                    ? graph.AdjacencyList.Keys.First()
                    : targetNode;

                StartSearchFromNode(startNode);
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void StartSearchFromNode(Node<T> startNode) {
            try {
                if (searchTimer.IsEnabled) return;

                visitedNodes = new HashSet<Node<T>>();
                animatedNodes.Clear();
                parentNodes.Clear();
                targetFound = false;
                this.startNode = startNode;

                if (isBFS) {
                    bfsQueue = new Queue<Node<T>>();
                    bfsQueue.Enqueue(startNode);
                }
                else {
                    dfsStack = new Stack<Node<T>>();
                    dfsStack.Push(startNode);
                }
                visitedNodes.Add(startNode);

                foreach (var edge in edgeStates) {
                    SetEdgeState(edge.Key.Item1, edge.Key.Item2, "Default");
                }

                searchTimer.Start();
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void SetTargetNode(Node<T> node) {
            try {
                if (Equals(targetNode, node)) {
                    targetNode = default;
                    if (nodeShapes.TryGetValue(node, out Ellipse ellipse)) {
                        ellipse.Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 100));
                        ellipse.StrokeThickness = 1;
                    }
                    targetStatusText.Text = "Target: None";
                }
                else {
                    if (!Equals(targetNode, default(Node<T>)) && nodeShapes.TryGetValue(targetNode, out Ellipse oldTarget)) {
                        oldTarget.Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 100));
                        oldTarget.StrokeThickness = 1;
                    }
                    targetNode = node;
                    if (nodeShapes.TryGetValue(targetNode, out Ellipse ellipse)) {
                        ellipse.Stroke = new SolidColorBrush(Colors.Red);
                        ellipse.StrokeThickness = 3;
                    }
                    targetStatusText.Text = $"Target: {node.Object}";
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void DisplayNodeStats(Node<T> node) {
            try {
                int connectionCount = graph.AdjacencyList[node].Count;
                bool isConnected = IsGraphConnected();
                bool hasCycle = HasCycle();
                int graphOrder = GetGraphOrder();
                int graphSize = GetGraphSize();
                string graphType = GetGraphType();

                nodeStatsText.Text = $"Node Stats:\n" +
                                     $"Name: {node.Object}\n" +
                                     $"Connections: {connectionCount}\n" +
                                     $"Graph Order (Nodes): {graphOrder}\n" +
                                     $"Graph Size (Edges): {graphSize}\n" +
                                     $"{graphType}\n" +
                                     $"Graph Connected: {isConnected}\n" +
                                     $"Contains Cycle: {hasCycle}";
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        // Detect if the graph is connected using BFS
        private bool IsGraphConnected() {
            if (graph.AdjacencyList.Count == 0) return true; // Empty graph is trivially connected

            var startNode = graph.AdjacencyList.Keys.First();
            var visited = new HashSet<Node<T>>();
            var queue = new Queue<Node<T>>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0) {
                var current = queue.Dequeue();
                foreach (var edge in graph.AdjacencyList[current]) {
                    var neighbor = edge.Item1;
                    if (!visited.Contains(neighbor)) {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited.Count == graph.AdjacencyList.Count;
        }

        private int GetGraphOrder() {
            return graph.AdjacencyList.Count; // Number of nodes
        }

        private int GetGraphSize() {
            // Count unique edges (since it's undirected, avoid double-counting)
            HashSet<(Node<T>, Node<T>)> uniqueEdges = new HashSet<(Node<T>, Node<T>)>();
            foreach (var node in graph.AdjacencyList.Keys) {
                foreach (var edge in graph.AdjacencyList[node]) {
                    var from = node;
                    var to = edge.Item1;
                    var edgeKey = Comparer<T>.Create((a, b) => a.ToString().CompareTo(b.ToString())).Compare(from.Object, to.Object) < 0
                        ? (from, to)
                        : (to, from);
                    uniqueEdges.Add(edgeKey);
                }
            }
            return uniqueEdges.Count; // Number of unique edges
        }

        private string GetGraphType() {
            bool isDirected = false; // Assume undirected unless proven otherwise
            bool isWeighted = false; // Assume unweighted unless weights are present

            isDirected = false; // Since DrawEdge adds edges symmetrically

            foreach (var node in graph.AdjacencyList.Keys) {
                foreach (var edge in graph.AdjacencyList[node]) {
                    if (!object.Equals(edge.Item2, default(T)) && edge.Item2 != 1) {
                        isWeighted = true;
                        break;
                    }
                }
                if (isWeighted) break;
            }

            return $"Type: {(isDirected ? "Directed" : "Undirected")}, {(isWeighted ? "Weighted" : "Unweighted")}";
        }

        // Detect if the graph has a cycle using DFS
        private bool HasCycle() {
            var visited = new HashSet<Node<T>>();
            var recStack = new HashSet<Node<T>>();
            var parent = new Dictionary<Node<T>, Node<T>>();

            foreach (var node in graph.AdjacencyList.Keys) {
                if (!visited.Contains(node)) {
                    if (DFSHasCycle(node, visited, recStack, parent))
                        return true;
                }
            }
            return false;
        }

        private bool DFSHasCycle(Node<T> node, HashSet<Node<T>> visited, HashSet<Node<T>> recStack, Dictionary<Node<T>, Node<T>> parent) {
            visited.Add(node);
            recStack.Add(node);

            foreach (var edge in graph.AdjacencyList[node]) {
                var neighbor = edge.Item1;
                if (!visited.Contains(neighbor)) {
                    parent[neighbor] = node;
                    if (DFSHasCycle(neighbor, visited, recStack, parent))
                        return true;
                }
                else if (recStack.Contains(neighbor) && !Equals(parent.GetValueOrDefault(node), neighbor)) {
                    return true; // Back edge found
                }
            }

            recStack.Remove(node);
            return false;
        }
        private void OnSearchTick(object sender, EventArgs e) {
            try {
                // If target is already found, do nothing and exit
                if (targetFound) {
                    return;
                }

                // If queue/stack is empty, search is complete
                if ((isBFS && bfsQueue.Count == 0) || (!isBFS && dfsStack.Count == 0)) {
                    searchTimer.Stop();
                    ResetColors();

                    var allPaths = ReconstructAllPaths();
                    var pathText = string.Empty;

                    // If a target exists and was found, display only the path to the target
                    if (!object.Equals(targetNode, default(Node<T>)) && targetFound && allPaths.ContainsKey(targetNode)) {
                        var targetPath = allPaths[targetNode];
                        pathText = $"Path to Target ({targetNode.Object}):\n{string.Join(" -> ", targetPath.Select(n => n.Object.ToString()))}";
                    }
                    // Otherwise, display all paths
                    else {
                        pathText = "Search Paths:\n";
                        foreach (var kvp in allPaths) {
                            var node = kvp.Key;
                            var path = kvp.Value;
                            pathText += $"{node.Object}: {string.Join(" -> ", path.Select(n => n.Object.ToString()))}\n";
                        }
                        pathText = pathText.TrimEnd('\n');
                    }

                    nodeStatsText.Text = pathText;
                    Logger.Log($"Search completed. Paths displayed:\n{pathText}");
                    return;
                }

                bool targetReached = false;
                if (isBFS) {
                    targetReached = ProcessBFSLayer();
                }
                else {
                    targetReached = ProcessDFSStep();
                }

                // If target is reached, stop immediately and handle visuals
                if (targetReached && !object.Equals(targetNode, default(Node<T>)) && animatedNodes.Contains(targetNode)) {
                    targetFound = true;
                    searchTimer.Stop(); // Stop timer immediately to prevent extra ticks
                    HighlightTarget();
                    HighlightShortestPath();
                    Logger.Log($"Target {targetNode.Object} found");

                    // Use a delay timer for resetting visuals and showing paths
                    DispatcherTimer stopDelayTimer = new DispatcherTimer {
                        Interval = TimeSpan.FromMilliseconds(1500)
                    };
                    stopDelayTimer.Tick += (s, args) => {
                        ResetColors();

                        var allPaths = ReconstructAllPaths();
                        var pathText = string.Empty;

                        // If a target exists and was found, display only the path to the target
                        if (!object.Equals(targetNode, default(Node<T>)) && targetFound && allPaths.ContainsKey(targetNode)) {
                            var targetPath = allPaths[targetNode];
                            pathText = $"Path to Target ({targetNode.Object}):\n{string.Join(" -> ", targetPath.Select(n => n.Object.ToString()))}";
                        }
                        // Otherwise, display all paths
                        else {
                            pathText = "Search Paths:\n";
                            foreach (var kvp in allPaths) {
                                var node = kvp.Key;
                                var path = kvp.Value;
                                pathText += $"{node.Object}: {string.Join(" -> ", path.Select(n => n.Object.ToString()))}\n";
                            }
                            pathText = pathText.TrimEnd('\n');
                        }

                        nodeStatsText.Text = pathText;
                        Logger.Log($"Target found. Paths displayed:\n{pathText}");

                        stopDelayTimer.Stop();
                    };
                    stopDelayTimer.Start();
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
                searchTimer.Stop();
            }
        }
        private Dictionary<Node<T>, List<Node<T>>> ReconstructAllPaths() {
            var paths = new Dictionary<Node<T>, List<Node<T>>>();
            foreach (var node in visitedNodes) {
                var path = new List<Node<T>>();
                Node<T> current = node;
                path.Add(current);
                while (!object.Equals(current, startNode) && !object.Equals(current, default(Node<T>)) && parentNodes.ContainsKey(current)) {
                    current = parentNodes[current];
                    path.Add(current);
                }
                path.Reverse(); // Start -> node order
                paths[node] = path;
            }
            return paths;
        }

        private void HighlightShortestPath() {
            try {
                if (!targetFound || Equals(targetNode, default(Node<T>)) || Equals(startNode, default(Node<T>))) {
                    Logger.Log("Cannot highlight path: Target or start node not set");
                    return;
                }

                // Reconstruct the path from target to start using parentNodes
                List<Node<T>> path = new List<Node<T>>();
                Node<T> current = targetNode;
                path.Add(current); // Include target node even if it's the start
                while (!Equals(current, startNode) && !Equals(current, default(Node<T>)) && parentNodes.ContainsKey(current)) {
                    current = parentNodes[current];
                    path.Add(current);
                }
                if (!path.Contains(startNode)) {
                    Logger.Log("Path does not include start node; incomplete path");
                }
                path.Reverse(); // Reverse to get start -> target order
                Logger.Log($"Reconstructed path: {string.Join(" -> ", path.Select(n => n.Object.ToString()))}");

                // Reset all nodes to default state except the target
                foreach (var node in nodeShapes.Keys) {
                    if (!Equals(node, targetNode)) // Leave target node green (handled by HighlightTarget)
                    {
                        var ellipse = nodeShapes[node];
                        ellipse.Fill = new SolidColorBrush(Color.FromRgb(20, 20, 35));
                        ellipse.Stroke = Equals(node, targetNode) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Color.FromRgb(80, 80, 100));
                        ellipse.StrokeThickness = Equals(node, targetNode) ? 3 : 1;
                        ellipse.RenderTransform = null;

                        if (nodeLabels.TryGetValue(node, out TextBlock text)) {
                            text.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                        }
                    }
                }

                // Highlight the edges in the path
                for (int i = 0; i < path.Count - 1; i++) {
                    Node<T> from = path[i];
                    Node<T> to = path[i + 1];
                    var edgeKey = Comparer<T>.Create((a, b) => a.ToString().CompareTo(b.ToString())).Compare(from.Object, to.Object) < 0 ? (from, to) : (to, from);
                    if (edgeStates.TryGetValue(edgeKey, out var edgeData)) {
                        var line = edgeData.Line;
                        line.Stroke = new SolidColorBrush(Colors.Yellow); // Distinct color for path
                        line.StrokeThickness = 4;                         // Thicker line for visibility
                        line.Opacity = 1;
                        line.BeginAnimation(Line.OpacityProperty, null);  // Clear any fading animations
                        edgeStates[edgeKey] = (line, "Path");
                        Logger.Log($"Highlighted edge: {from.Object} -> {to.Object}");
                    }
                    else {
                        Logger.Log($"Edge not found in edgeStates: {from.Object} -> {to.Object}");
                    }
                }

                // Reset all edges not in the path to default
                foreach (var edge in edgeStates) {
                    var from = edge.Key.Item1;
                    var to = edge.Key.Item2;
                    if (!path.Contains(from) || !path.Contains(to) || !path.Zip(path.Skip(1), (a, b) => (a, b)).Any(p => (p.a == from && p.b == to) || (p.a == to && p.b == from))) {
                        SetEdgeState(from, to, "Default");
                    }
                }

                Logger.Log($"Shortest path highlighted with {path.Count} nodes");
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }
        private bool ProcessDFSStep() {
            try {
                var currentNode = dfsStack.Pop();
                bool targetReached = false;

                if (!animatedNodes.Contains(currentNode)) {
                    HighlightNode(currentNode);
                    animatedNodes.Add(currentNode);
                    if (object.Equals(currentNode, targetNode)) // Use object.Equals
                    {
                        targetReached = true;
                    }
                }

                if (targetReached) // Exit early if target is found
                {
                    return true;
                }

                var neighbors = graph.AdjacencyList[currentNode].Select(e => e.Item1).Reverse();
                foreach (var neighbor in neighbors) {
                    if (!visitedNodes.Contains(neighbor)) {
                        visitedNodes.Add(neighbor);
                        dfsStack.Push(neighbor);
                        parentNodes[neighbor] = currentNode;
                        DispatcherTimer edgeTimer = new DispatcherTimer {
                            Interval = TimeSpan.FromMilliseconds(200)
                        };
                        edgeTimer.Tick += (s, e) => {
                            SetEdgeState(currentNode, neighbor, "Current");
                            edgeTimer.Stop();
                        };
                        edgeTimer.Start();
                    }
                }

                if (parentNodes.ContainsKey(currentNode)) {
                    SetEdgeState(parentNodes[currentNode], currentNode, "Explored");
                }

                foreach (var edge in graph.AdjacencyList[currentNode]) {
                    Node<T> neighbor = edge.Item1;
                    if (visitedNodes.Contains(neighbor) && !dfsStack.Contains(neighbor) && !object.Equals(neighbor, currentNode)) {
                        SetEdgeState(currentNode, neighbor, "Explored");
                    }
                }

                foreach (var node in animatedNodes.Except(new[] { currentNode })) {
                    DimNode(node);
                }

                return targetReached;
            }
            catch (Exception ex) {
                Logger.Log(ex);
                return false;
            }
        }
        private bool ProcessBFSLayer() {
            try {
                int nodesInCurrentLayer = bfsQueue.Count;
                var currentLayer = new List<Node<T>>();
                bool targetReached = false;

                for (int i = 0; i < nodesInCurrentLayer && bfsQueue.Count > 0 && !targetReached; i++) // Added !targetReached to stop early
                {
                    var currentNode = bfsQueue.Dequeue();
                    currentLayer.Add(currentNode);

                    if (!animatedNodes.Contains(currentNode)) {
                        HighlightNode(currentNode);
                        animatedNodes.Add(currentNode);
                        if (object.Equals(currentNode, targetNode)) // Use object.Equals for consistency
                        {
                            targetReached = true;
                        }
                    }

                    foreach (var edge in graph.AdjacencyList[currentNode]) {
                        Node<T> neighbor = edge.Item1;
                        if (!visitedNodes.Contains(neighbor)) {
                            visitedNodes.Add(neighbor);
                            bfsQueue.Enqueue(neighbor);
                            parentNodes[neighbor] = currentNode;
                            DispatcherTimer edgeTimer = new DispatcherTimer {
                                Interval = TimeSpan.FromMilliseconds(200)
                            };
                            edgeTimer.Tick += (s, e) => {
                                SetEdgeState(currentNode, neighbor, "Current");
                                edgeTimer.Stop();
                            };
                            edgeTimer.Start();
                        }
                    }

                    if (parentNodes.ContainsKey(currentNode)) {
                        SetEdgeState(parentNodes[currentNode], currentNode, "Explored");
                    }

                    foreach (var edge in graph.AdjacencyList[currentNode]) {
                        Node<T> neighbor = edge.Item1;
                        if (visitedNodes.Contains(neighbor) && (bfsQueue.Count == 0 || !object.Equals(neighbor, PeekOrDefault(bfsQueue))) && !object.Equals(neighbor, currentNode)) {
                            SetEdgeState(currentNode, neighbor, "Explored");
                        }
                    }
                }

                foreach (var node in animatedNodes.Except(currentLayer)) {
                    DimNode(node);
                }

                return targetReached;
            }
            catch (Exception ex) {
                Logger.Log(ex);
                return false;
            }
        }

        private Node<T> PeekOrDefault(Queue<Node<T>> queue) {
            try {
                return queue.Count > 0 ? queue.Peek() : default;
            }
            catch (Exception ex) {
                Logger.Log(ex);
                return default;
            }
        }

        private void ResetSearch(object sender, RoutedEventArgs e) {
            try {
                searchTimer.Stop();
                visitedNodes.Clear();
                animatedNodes.Clear();
                parentNodes.Clear();
                targetFound = false;
                if (isBFS && bfsQueue != null) bfsQueue.Clear();
                if (!isBFS && dfsStack != null) dfsStack.Clear();
                ResetColors();
                Logger.Log("Search reset");
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void SetEdgeState(Node<T> from, Node<T> to, string state) {
            try {
                var edgeKey = Comparer<T>.Create((a, b) => a.ToString().CompareTo(b.ToString())).Compare(from.Object, to.Object) < 0 ? (from, to) : (to, from);
                if (edgeStates.TryGetValue(edgeKey, out var edgeData)) {
                    var line = edgeData.Line;
                    switch (state) {
                        case "Current":
                            line.Stroke = new SolidColorBrush(Color.FromRgb(255, 200, 100));
                            line.StrokeThickness = 2;
                            line.Opacity = 0;
                            var fadeInAnimation = new DoubleAnimation {
                                From = 0,
                                To = 1,
                                Duration = TimeSpan.FromMilliseconds(300)
                            };
                            line.BeginAnimation(Line.OpacityProperty, fadeInAnimation);
                            var thicknessAnimation = new DoubleAnimation {
                                From = 2,
                                To = 3,
                                Duration = TimeSpan.FromMilliseconds(500),
                                AutoReverse = true
                            };
                            line.BeginAnimation(Line.StrokeThicknessProperty, thicknessAnimation);
                            break;
                        case "Explored":
                            line.Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 100));
                            line.StrokeThickness = 1;
                            line.Opacity = 0;
                            line.BeginAnimation(Line.StrokeThicknessProperty, null);
                            line.BeginAnimation(Line.OpacityProperty, null);
                            break;
                        case "Default":
                            line.Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 100));
                            line.StrokeThickness = 1;
                            line.Opacity = 1;
                            line.BeginAnimation(Line.StrokeThicknessProperty, null);
                            line.BeginAnimation(Line.OpacityProperty, null);
                            break;
                    }
                    edgeStates[edgeKey] = (line, state);
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void HighlightNode(Node<T> node) {
            try {
                if (nodeShapes.TryGetValue(node, out Ellipse ellipse)) {
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(255, 200, 100));
                    var scaleAnimation = new DoubleAnimation {
                        From = 1.0,
                        To = 1.3,
                        Duration = TimeSpan.FromMilliseconds(800),
                        AutoReverse = true
                    };
                    var transform = new ScaleTransform(1, 1, ellipse.Width / 2, ellipse.Height / 2);
                    ellipse.RenderTransform = transform;
                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

                    if (nodeLabels.TryGetValue(node, out TextBlock text)) {
                        text.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void HighlightTarget() {
            try {
                if (!Equals(targetNode, default(Node<T>)) && nodeShapes.TryGetValue(targetNode, out Ellipse ellipse)) {
                    ellipse.Fill = new SolidColorBrush(Colors.Green);
                    ellipse.Stroke = new SolidColorBrush(Colors.Red);
                    ellipse.StrokeThickness = 3;
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void DimNode(Node<T> node) {
            try {
                if (nodeShapes.TryGetValue(node, out Ellipse ellipse)) {
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(100, 100, 150));
                    ellipse.RenderTransform = null;
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        private void ResetColors() {
            try {
                foreach (var node in nodeShapes.Keys) {
                    var ellipse = nodeShapes[node];
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(20, 20, 35));
                    ellipse.Stroke = Equals(node, targetNode) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Color.FromRgb(80, 80, 100));
                    ellipse.StrokeThickness = Equals(node, targetNode) ? 3 : 1;
                    ellipse.RenderTransform = null;

                    if (nodeLabels.TryGetValue(node, out TextBlock text)) {
                        text.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                    }
                }

                // Preserve "Path" state edges
                foreach (var edge in edgeStates) {
                    if (edge.Value.State != "Path") {
                        SetEdgeState(edge.Key.Item1, edge.Key.Item2, "Default");
                    }
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }
    }
}