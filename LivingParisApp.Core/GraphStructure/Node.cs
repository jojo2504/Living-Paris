namespace LivingParisApp.Core.GraphStructure {
    /// <summary>
    /// A node will likely represent a metro station in the context of this project
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Node<T> {
        public T Object { get; }
        public Node(T obj) => Object = obj;
        
        /// <summary>
        /// Returns a string representation of the node.
        /// </summary>
        /// <returns>String representation of the node or "null" if the object is null.</returns>
        public override string ToString() {
            return Object?.ToString() ?? "null"; // Safely handle null Object
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current node.
        /// </summary>
        /// <param name="obj">The object to compare with the current node.</param>
        /// <returns>True if the objects are equal; otherwise, false.</returns>
        public override bool Equals(object obj) {
            if (obj is Node<T> other)
                return EqualityComparer<T>.Default.Equals(Object, other.Object);
            return false;
        }

        /// <summary>
        /// Returns a hash code for the current node.
        /// </summary>
        /// <returns>A hash code based on the node's Object property.</returns>
        public override int GetHashCode()
        {
            return Object is null ? 0 : EqualityComparer<T>.Default.GetHashCode(Object);
        }
    }
}