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

        public override bool Equals(object obj) {
            if (obj is Node<T> other) {
                return EqualityComparer<T>.Default.Equals(this.Object, other.Object);
            }
            return false;
        }   

        public override int GetHashCode() {
            return EqualityComparer<T>.Default.GetHashCode(this.Object);
        }

        public override string ToString() {
            throw new NotImplementedException();
        }
    }
}