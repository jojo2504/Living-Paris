using System.Text;
using LivingParisApp.Core.GraphStructure;

namespace LivingParisApp.Core.Mapping {
    public class RelationshipMap<T> : Graph<T> {
        public override string ToString() {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<Node<T>, List<Tuple<Node<T>, double>>> kpv in AdjacencyList) {
                stringBuilder.Append($"{kpv.Key.Object} => ");
                foreach (var node in kpv.Value) {
                    stringBuilder.Append($"{node.Item1.Object}({node.Item2}), ");
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