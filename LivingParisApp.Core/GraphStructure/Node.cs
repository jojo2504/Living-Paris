namespace LivingParisApp.Core.GraphStructure {
    /// <summary>
    /// A node will likely represent a metro station in the context of this project
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Node<T> {
        public T Object { get; }
        public Node(T obj) => Object = obj;

        public override string ToString() {
            return Object?.ToString() ?? "null"; // Safely handle null Object
        }

        // Optional: Override Equals and GetHashCode for dictionary key usage
        public override bool Equals(object obj) {
            if (obj is Node<T> other)
                return EqualityComparer<T>.Default.Equals(Object, other.Object);
            return false;
        }

        public override int GetHashCode() {
            return Object != null ? Object.GetHashCode() : 0;
        }
    }
}