namespace LivingParisApp.Core.GraphStructure {
    public enum Direction {
        Undirected, // A <=> B
        Direct,     // A => B
        Indirect    // B => A
    }

    public class Link<T>{
        public Node<T> A {get;}
        public Node<T> B {get;}
        public Direction Direction {get;}
        public double Weight {get;}
        public Link(Node<T> A, Node<T> B, Direction direction, double weight) {
            this.A = A;
            this.B = B;
            Direction = direction;
            Weight = weight;
        }
    }
}   