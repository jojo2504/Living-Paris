using LivingParisApp.Core.Entities.Station;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp.Core.Engines.ShortestPaths {
    public class Astar<T> : ShortestPathsEngine<T> where T : IStation<T> {
        public (LinkedList<Node<T>> Path, double TotalLength) Run(Map<T> map, Node<T> from, Node<T> to) {
            var cameFrom = new Dictionary<Node<T>, Node<T>>();
            var gScore = new Dictionary<Node<T>, double>();
            var fScore = new Dictionary<Node<T>, double>();
            var openSet = new SortedSet<Node<T>>(Comparer<Node<T>>.Create((a, b) => fScore[a].CompareTo(fScore[b])));

            foreach (var node in map.AdjacencyList.Keys) {
                gScore[node] = double.PositiveInfinity;
                fScore[node] = double.PositiveInfinity;
            }
            
            gScore[from] = 0;
            fScore[from] = Heuristic(from.Object, to.Object);
            openSet.Add(from);
            
            while (openSet.Count > 0) {
                var current = openSet.First();
                openSet.Remove(current);
                
                if (current.Equals(to)) {
                    return (ReconstructPath(cameFrom, current), gScore[to]);
                }
                
                if (!map.AdjacencyList.ContainsKey(current)) continue;
                
                foreach (var (neighbor, weight) in map.AdjacencyList[current]) {
                    double tentativeGScore = gScore[current] + weight;
                    
                    if (tentativeGScore < gScore[neighbor]) {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor.Object, to.Object);
                        if (!openSet.Contains(neighbor)) {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            return (new LinkedList<Node<T>>(), double.PositiveInfinity); // No path found
        }

        private double Heuristic(T a, T b) {
            return a.CalculateDistance(b);
        }

        private LinkedList<Node<T>> ReconstructPath(Dictionary<Node<T>, Node<T>> cameFrom, Node<T> current) {
            var path = new LinkedList<Node<T>>();
            while (cameFrom.ContainsKey(current)) {
                path.AddFirst(current);
                current = cameFrom[current];
            }
            path.AddFirst(current);
            return path;
        }
    }
}
