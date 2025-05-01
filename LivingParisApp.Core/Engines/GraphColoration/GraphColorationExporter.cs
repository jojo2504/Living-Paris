using System.Text.Json;
using LivingParisApp.Core.GraphStructure;

namespace LivingParisApp.Core.Engines.GraphColoration {
    public class GraphColorationExporter<T> {
        private WelshPowell<T> _welshPowell;

        public GraphColorationExporter(WelshPowell<T> welshPowell) {
            _welshPowell = welshPowell ?? throw new ArgumentNullException(nameof(welshPowell));
        }

        public string ExportToJson() {
            var colorAssignment = _welshPowell.ColorGraph();
            var colorGroups = _welshPowell.GetNodesGroupedByColor();

            var exportData = new {
                ColorAssignment = ConvertColorAssignment(colorAssignment),
                ColorCount = _welshPowell.GetColorCount(),
                IsBipartite = _welshPowell.IsBipartite(),
                IsPlanar = _welshPowell.IsPlanar(),
                GroupedByColor = ConvertColorGroups(colorGroups)
            };

            var options = new JsonSerializerOptions {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(exportData, options);
        }

        private Dictionary<string, int> ConvertColorAssignment(Dictionary<Node<T>, int> original) {
            var result = new Dictionary<string, int>();
            foreach (var kvp in original) {
                result[kvp.Key.ToString()] = kvp.Value;
            }
            return result;
        }

        private Dictionary<int, List<string>> ConvertColorGroups(Dictionary<int, List<Node<T>>> groups) {
            var result = new Dictionary<int, List<string>>();
            foreach (var kvp in groups) {
                result[kvp.Key] = new List<string>();
                foreach (var node in kvp.Value) {
                    result[kvp.Key].Add(node.ToString());
                }
            }
            return result;
        }
    }
}
