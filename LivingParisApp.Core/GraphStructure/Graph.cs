using LivingParisApp.Services.FileHandling;

namespace LivingParisApp.Core.GraphStructure {
    public class Graph<T> {
        public Dictionary<Node<T>, List<Tuple<Node<T>, int>>> AdjacencyList { get; private set; }
        public bool[,] AdjacencyMatrix { get; private set; }

        public Graph() {
            throw new NotImplementedException();
        }

        public void CreateAdjacencyList(string FilePath){
            AdjacencyList = new Dictionary<Node<T>, List<Tuple<Node<T>, int>>>();
            FileReader fileReader = new FileReader(FilePath);

            throw new NotImplementedException();
        }
    }
}