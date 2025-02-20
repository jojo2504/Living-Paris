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
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Diagnostics;
using LivingParisApp.Services.Logging;

namespace LivingParisApp {
    public partial class MainWindow : Window {
        private Dictionary<Node<string>, Point> nodePositions = new();
        private Dictionary<Node<string>, Ellipse> nodeShapes = new();
        private Dictionary<Node<string>, TextBlock> nodeLabels = new();
        private Dictionary<Node<string>, List<Line>> edgeLines = new();

        private Rectangle nightModeOverlay;

        private Node<string> draggingNode = null;
        private Point lastMousePosition;

        // BFS variables
        private Queue<Node<string>> bfsQueue;
        private DispatcherTimer bfsTimer;
        private HashSet<Node<string>> visitedNodes;
        private HashSet<Node<string>> animatedNodes = new();
        private Dictionary<Line, DropShadowEffect> edgeEffects;
        private HashSet<(Node<string>, Node<string>)> animatedEdges = new();  // Set to track animated edges

        private Graph<string> graph;

        public MainWindow(Graph<string> graph) {
            InitializeComponent();
            this.graph = graph;
            DrawGraph(graph);

            bfsTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(2000) // Animation speed
            };
            bfsTimer.Tick += OnBfsTick;
        }

        private void InitializeNightMode() {
            // Create a dark overlay to simulate night mode
            nightModeOverlay = new Rectangle {
                Width = GraphCanvas.ActualWidth,
                Height = GraphCanvas.ActualHeight,
                Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), // Semi-transparent black
                IsHitTestVisible = false // Allows interaction with nodes
            };

            // Add overlay as the first child (background layer)
            GraphCanvas.Children.Insert(0, nightModeOverlay);
        }

        private void DrawGraph(Graph<string> graph) {
            try {
                Random rand = new Random();

                // Assign random initial positions to nodes
                foreach (var node in graph.AdjacencyList.Keys) {
                    if (!nodePositions.ContainsKey(node)) {
                        nodePositions[node] = new Point(rand.Next(100, 1880), rand.Next(100, 980));
                    }
                }

                // Draw edges
                HashSet<(Node<string>, Node<string>)> drawnEdges = new();
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

                // Draw nodes
                foreach (var node in graph.AdjacencyList.Keys) {
                    DrawNode(node);
                }
            }
            catch (Exception ex) {
                Logger.Log(ex.Message);
            }
        }


        private void DrawNode(Node<string> node) {
            Point position = nodePositions[node];

            Canvas nodeContainer = new Canvas {
                Width = 100,
                Height = 100,
                ClipToBounds = false
            };

            // Create a much darker base node
            Ellipse ellipse = new Ellipse {
                Width = 60,
                Height = 60,
                StrokeThickness = 2,
                Stroke = new SolidColorBrush(Color.FromArgb(40, 100, 100, 100)), // Very faint outline
                Fill = new RadialGradientBrush {
                    GradientStops = new GradientStopCollection {
                new GradientStop(Color.FromArgb(30, 20, 20, 35), 0.0), // Almost black with slight blue tint
                new GradientStop(Color.FromArgb(20, 10, 10, 25), 1.0)  // Even darker edge
            }
                },
                Effect = new DropShadowEffect {
                    Color = Colors.Black,
                    BlurRadius = 5,
                    ShadowDepth = 0,
                    Opacity = 0.2
                }
            };

            Canvas.SetLeft(ellipse, 20);
            Canvas.SetTop(ellipse, 20);

            // Darker, more subtle text
            TextBlock text = new TextBlock {
                Text = node.Object.ToString(),
                Foreground = new SolidColorBrush(Color.FromArgb(120, 200, 200, 200)), // Very faint text
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Effect = new DropShadowEffect {
                    Color = Colors.Black,
                    BlurRadius = 3,
                    ShadowDepth = 0,
                    Opacity = 0.8
                },
                Background = null,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size textSize = text.DesiredSize;
            Canvas.SetLeft(text, (60 - textSize.Width) / 2 + 20);
            Canvas.SetTop(text, (60 - textSize.Height) / 2 + 20);

            nodeContainer.Children.Add(ellipse);
            nodeContainer.Children.Add(text);

            Canvas.SetLeft(nodeContainer, position.X - 50);
            Canvas.SetTop(nodeContainer, position.Y - 50);

            // Event handlers remain the same
            ellipse.MouseDown += (sender, e) => {
                if (e.ChangedButton == MouseButton.Left && e.ClickCount >= 2) {
                    StartBFS(node);
                }
            };

            nodeContainer.MouseDown += (sender, e) => StartDragging(node, e);
            nodeContainer.MouseMove += (sender, e) => DragNode(e);
            nodeContainer.MouseUp += (sender, e) => StopDragging();

            GraphCanvas.Children.Add(nodeContainer);
            nodeShapes[node] = ellipse;
            nodeLabels[node] = text;
        }

        private void DrawEdge(Node<string> from, Node<string> to) {
            // Increased edge opacity from 25 to 40 for better visibility
            Line line = new Line {
                X1 = nodePositions[from].X,
                Y1 = nodePositions[from].Y,
                X2 = nodePositions[to].X,
                Y2 = nodePositions[to].Y,
                Stroke = new SolidColorBrush(Color.FromArgb(40, 80, 80, 100)), // Increased opacity and slightly lighter color
                StrokeThickness = 1.5
            };

            GraphCanvas.Children.Add(line);

            if (!edgeLines.ContainsKey(from)) edgeLines[from] = new List<Line>();
            if (!edgeLines.ContainsKey(to)) edgeLines[to] = new List<Line>();

            edgeLines[from].Add(line);
            edgeLines[to].Add(line);
        }

        private void StartDragging(Node<string> node, MouseButtonEventArgs e) {
            draggingNode = node;
            lastMousePosition = e.GetPosition(GraphCanvas);
        }

        private void DragNode(MouseEventArgs e) {
            if (draggingNode == null) return;

            Point newMousePosition = e.GetPosition(GraphCanvas);
            double dx = newMousePosition.X - lastMousePosition.X;
            double dy = newMousePosition.Y - lastMousePosition.Y;

            if (nodeShapes.TryGetValue(draggingNode, out Ellipse ellipse)) {
                Canvas nodeContainer = (Canvas)ellipse.Parent;
                double newX = Canvas.GetLeft(nodeContainer) + dx;
                double newY = Canvas.GetTop(nodeContainer) + dy;

                // Update container position and node position
                Canvas.SetLeft(nodeContainer, newX);
                Canvas.SetTop(nodeContainer, newY);
                nodePositions[draggingNode] = new Point(newX + 50, newY + 50);
                lastMousePosition = newMousePosition;

                UpdateAllEdges(); // Update edges immediately
            }
        }

        private void StopDragging() {
            draggingNode = null;
        }

        private void UpdateAllEdges() {
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

        private void StartBFS(Node<string> startNode) {
            if (bfsTimer.IsEnabled) return;

            bfsQueue = new Queue<Node<string>>();
            visitedNodes = new HashSet<Node<string>>();
            edgeEffects = new Dictionary<Line, DropShadowEffect>();
            animatedEdges.Clear();
            animatedNodes.Clear(); // Reset animation tracker

            bfsQueue.Enqueue(startNode);
            visitedNodes.Add(startNode);

            bfsTimer.Start(); // Start BFS animation
        }


        private void OnBfsTick(object sender, EventArgs e) {
            if (bfsQueue.Count == 0) {
                bfsTimer.Stop();
                ResetColors();
                return;
            }

            var currentLayer = new List<Node<string>>();
            var nextLayerQueue = new Queue<Node<string>>();

            while (bfsQueue.Count > 0) {
                var currentNode = bfsQueue.Dequeue();

                // Highlight the current node
                if (!animatedNodes.Contains(currentNode)) {
                    HighlightNode(currentNode);
                    animatedNodes.Add(currentNode);

                    // Find and highlight all edges where currentNode is the target
                    foreach (var sourceNode in graph.AdjacencyList.Keys) {
                        foreach (var edge in graph.AdjacencyList[sourceNode]) {
                            if (edge.Item1 == currentNode) {
                                var edgePair = string.Compare(sourceNode.Object, currentNode.Object, StringComparison.Ordinal) < 0
                                    ? (sourceNode, currentNode)
                                    : (currentNode, sourceNode);

                                if (!animatedEdges.Contains(edgePair)) {
                                    HighlightEdge(sourceNode, currentNode);
                                    animatedEdges.Add(edgePair);
                                }
                            }
                        }
                    }
                }
                currentLayer.Add(currentNode);

                // Queue up neighbors for next layer
                foreach (var edge in graph.AdjacencyList[currentNode]) {
                    Node<string> neighbor = edge.Item1;
                    if (!visitedNodes.Contains(neighbor)) {
                        visitedNodes.Add(neighbor);
                        nextLayerQueue.Enqueue(neighbor);
                    }
                }
            }

            // Move to the next BFS layer
            if (nextLayerQueue.Count > 0) {
                bfsQueue = nextLayerQueue;
                foreach (var node in currentLayer) {
                    DimNodeBrightness(node);
                }
            }
        }

        private void HighlightNode(Node<string> node) {
            if (nodeShapes.TryGetValue(node, out Ellipse ellipse)) {
                // Dramatic contrast with bright illumination
                var glowEffect = new DropShadowEffect {
                    Color = Colors.Yellow,
                    BlurRadius = 35,
                    ShadowDepth = 0,
                    Opacity = 0.9
                };

                ellipse.Effect = glowEffect;

                // Larger, more prominent halo
                var halo = new Ellipse {
                    Width = 250,
                    Height = 250,
                    Fill = new RadialGradientBrush {
                        GradientStops = new GradientStopCollection {
                    new GradientStop(Color.FromArgb(120, 255, 255, 150), 0.0),
                    new GradientStop(Color.FromArgb(0, 255, 255, 150), 1.0)
                }
                    }
                };

                Canvas.SetLeft(halo, nodePositions[node].X - 125);
                Canvas.SetTop(halo, nodePositions[node].Y - 125);
                GraphCanvas.Children.Add(halo);

                // More dramatic pulsing effect
                var lightAnimation = new DoubleAnimation {
                    From = 1.0,
                    To = 0.4,
                    Duration = TimeSpan.FromSeconds(1.2),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };
                halo.BeginAnimation(Ellipse.OpacityProperty, lightAnimation);

                // Brighter node illumination
                ellipse.Fill = new RadialGradientBrush {
                    GradientStops = new GradientStopCollection {
                new GradientStop(Color.FromRgb(255, 235, 150), 0.0),
                new GradientStop(Color.FromRgb(255, 200, 100), 1.0)
            }
                };

                // Make text more visible during highlight
                if (nodeLabels.TryGetValue(node, out TextBlock text)) {
                    text.Foreground = new SolidColorBrush(Colors.White);
                    text.Effect = new DropShadowEffect {
                        Color = Colors.Black,
                        BlurRadius = 5,
                        ShadowDepth = 0,
                        Opacity = 1
                    };
                }

                Task.Delay(1500).ContinueWith(_ => Dispatcher.Invoke(() => {
                    FadeOutNode(node, glowEffect);
                    GraphCanvas.Children.Remove(halo);

                    // Reset text to dark state
                    if (nodeLabels.TryGetValue(node, out TextBlock textBlock)) {
                        textBlock.Foreground = new SolidColorBrush(Color.FromArgb(60, 200, 200, 200));
                        textBlock.Effect = new DropShadowEffect {
                            Color = Colors.Black,
                            BlurRadius = 3,
                            ShadowDepth = 0,
                            Opacity = 0.8
                        };
                    }
                }));
            }
        }

        private void FadeOutNode(Node<string> node, DropShadowEffect glowEffect) {
            if (nodeShapes.TryGetValue(node, out Ellipse ellipse)) {
                // Fade out glow smoothly (Without reactivating it)
                var fadeOutAnimation = new DoubleAnimation {
                    From = glowEffect.Opacity,
                    To = 0.3,  // Dim brightness
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn }
                };

                glowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, fadeOutAnimation);

                // Reset color back to normal
                ellipse.Fill = new RadialGradientBrush(
                    Color.FromRgb(100, 181, 246),
                    Color.FromRgb(30, 136, 229)
                );
            }
        }

        private void HighlightEdge(Node<string> from, Node<string> to) {
            if (edgeLines.TryGetValue(from, out List<Line> lines)) {
                var line = lines.FirstOrDefault(l =>
                    (Math.Abs(l.X2 - nodePositions[to].X) <= 2 && Math.Abs(l.Y2 - nodePositions[to].Y) <= 2) ||
                    (Math.Abs(l.X1 - nodePositions[to].X) <= 2 && Math.Abs(l.Y1 - nodePositions[to].Y) <= 2));

                if (line != null) {
                    // Create a storyboard for coordinated animations
                    Storyboard storyboard = new Storyboard();

                    // Brighter edge illumination with more dramatic effect
                    line.Stroke = new SolidColorBrush(Color.FromArgb(255, 200, 255, 255));
                    line.StrokeThickness = 3;

                    // Enhanced glow effect
                    var glowEffect = new DropShadowEffect {
                        Color = Colors.Cyan,
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.7
                    };
                    line.Effect = glowEffect;

                    // Thickness animation
                    var thicknessAnimation = new DoubleAnimation {
                        From = 3,
                        To = 5,
                        Duration = TimeSpan.FromMilliseconds(500),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2)
                    };
                    Storyboard.SetTarget(thicknessAnimation, line);
                    Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath(Line.StrokeThicknessProperty));
                    storyboard.Children.Add(thicknessAnimation);

                    // Start the animations
                    storyboard.Begin();

                    // Fade out edge after delay
                    Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() => {
                        // Fade out animation
                        var fadeOutStoryboard = new Storyboard();
                        
                        var opacityAnimation = new DoubleAnimation {
                            From = 1.0,
                            To = 0.4,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
                        };

                        var brushAnimation = new ColorAnimation {
                            To = Color.FromArgb(40, 80, 80, 100),
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
                        };

                        Storyboard.SetTarget(opacityAnimation, line);
                        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("(Line.Effect).(DropShadowEffect.Opacity)"));
                        fadeOutStoryboard.Children.Add(opacityAnimation);

                        fadeOutStoryboard.Completed += (s, e) => {
                            line.Stroke = new SolidColorBrush(Color.FromArgb(40, 80, 80, 100));
                            line.StrokeThickness = 1.5;
                            line.Effect = null;
                        };

                        fadeOutStoryboard.Begin();
                    }));
                }
            }
        }
        private void DimNodeBrightness(Node<string> node) {
            if (nodeShapes.TryGetValue(node, out Ellipse ellipse)) {
                // Gradually reduce the brightness of the node's glow after each BFS layer
                var currentEffect = (DropShadowEffect)ellipse.Effect;
                if (currentEffect != null) {
                    var fadeOutAnimation = new DoubleAnimation {
                        From = currentEffect.Opacity,
                        To = 0.3,  // Dimming the brightness
                        Duration = TimeSpan.FromSeconds(1),
                        EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn }
                    };

                    currentEffect.BeginAnimation(DropShadowEffect.OpacityProperty, fadeOutAnimation);
                }
            }
        }

        private void ResetColors() {
            foreach (var node in nodeShapes.Keys) {
                var ellipse = nodeShapes[node];
                ellipse.Fill = new RadialGradientBrush {
                    GradientStops = new GradientStopCollection {
                new GradientStop(Color.FromArgb(30, 20, 20, 35), 0.0),
                new GradientStop(Color.FromArgb(20, 10, 10, 25), 1.0)
            }
                };

                ellipse.Effect = new DropShadowEffect {
                    Color = Colors.Black,
                    BlurRadius = 5,
                    ShadowDepth = 0,
                    Opacity = 0.2
                };

                if (nodeLabels.TryGetValue(node, out TextBlock text)) {
                    text.Foreground = new SolidColorBrush(Color.FromArgb(120, 200, 200, 200)); // Match the new text opacity
                }
            }

            foreach (var line in edgeLines.Values.SelectMany(l => l)) {
                line.Stroke = new SolidColorBrush(Color.FromArgb(40, 80, 80, 100)); // Match the new edge opacity
                line.StrokeThickness = 1.5;
                line.Effect = null;
                line.BeginAnimation(Line.StrokeThicknessProperty, null);
            }
        }
    }
}