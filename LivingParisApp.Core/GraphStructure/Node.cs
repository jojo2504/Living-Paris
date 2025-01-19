namespace LivingParisApp.Core.GraphStructure {
    /// <summary>
    /// A node will likely represent a metro station in the context of this project
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Node<T> {
        public readonly T Object;

        public Node(T Object) {
            this.Object = Object;
        }

        public override string ToString() {
            throw new NotImplementedException();
        }
    }
}