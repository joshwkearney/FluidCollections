using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace FluidCollections {
    internal class OrderedSet<T> : IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, IReadOnlyList<T> {
        private readonly IComparer<T> comparer;
        private Node root = null;
        private int version = 0;

        public int Count => root?.Size ?? 0;

        public T Max {
            get {
                if (this.root == null) {
                    return default;
                }
                else {
                    return this.Max_Core(this.root);
                }
            }
        }

        public T Min {
            get {
                if (this.root == null) {
                    return default;
                }
                else {                    
                    return this.Min_Core(this.root);
                }
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index] {
            get {
                if (index < 0 || index >= this.Count) throw new IndexOutOfRangeException();
                return this.Indexing_Core(this.root, index);
            }
        }

        public OrderedSet() {
            this.comparer = Comparer<T>.Default;
        }

        public OrderedSet(IComparer<T> comparer) {
            this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public bool Add(T item) {
            if (this.root == null) {
                this.root = new Node() {
                    Size = 1,
                    Value = item
                };

                this.version++;
                return true;
            }
            else {
                if (this.Add_Core(item, this.root)) {
                    return true;
                }

                return false;
            }
        }

        public bool Remove(T item) {
            if (this.root == null) {
                return false;
            }

            var (sucess, newRoot) = this.Remove_Core(item, this.root);

            this.root = newRoot;
            return sucess;
        }

        public int IndexOf(T item) {
            return this.IndexOf_Core(this.root, item, 0);
        }

        public bool RemoveAt(int index) {
            if (index < 0 || index >= this.Count) throw new IndexOutOfRangeException();

            var item = this[index];
            return this.Remove(item);
        }

        // Note - AddCore and RemoveCore aren't locked because they are only called from
        // Add and Remove, with are locked
        private bool Add_Core(T item, Node node) {
            bool result = false;

            // Add the node right or left
            if (this.comparer.Compare(item, node.Value) > 0) {
                if (node.Right == null) {
                    node.Right = new Node() {
                        Size = 1,
                        Value = item
                    };

                    result = true;
                    this.version++;
                }
                else {
                    result = this.Add_Core(item, node.Right);
                }
            }
            else if (this.comparer.Compare(item, node.Value) < 0) {
                if (node.Left == null) {
                    node.Left = new Node() {
                        Size = 1,
                        Value = item
                    };

                    this.version++;
                    result = true;
                }
                else {
                    result = this.Add_Core(item, node.Left);
                }
            }

            if (!result) {
                return false;
            }

            node.Size++;
            this.Balance(node);

            return true;
        }

        private (bool success, Node replacement) Remove_Core(T item, Node node) {
            bool success = false;

            if (this.comparer.Compare(item, node.Value) > 0) {
                if (node.Right != null) {
                    var (removed, replacement) = this.Remove_Core(item, node.Right);

                    if (removed) {
                        node.Right = replacement;
                        node.Size--;
                        success = true;
                    }
                }
            }
            else if (this.comparer.Compare(item, node.Value) < 0) {
                if (node.Left != null) {
                    var (removed, replacement) = this.Remove_Core(item, node.Left);

                    if (removed) {
                        node.Left = replacement;
                        node.Size--;
                        success = true;
                    }
                }
            }
            else {
                if (node.Left == null && node.Right == null) {
                    return (true, null);
                }
                else if (node.Left == null) {
                    return (true, node.Right);
                }
                else if (node.Right == null) {
                    return (true, node.Left);
                }
                else {
                    var min = this.Min_Core(node.Right);
                    var (removed, replacement) = this.Remove_Core(min, node.Right);

                    if (!removed) {
                        throw new InvalidOperationException();
                    }

                    node.Value = min;
                    node.Right = replacement;
                    node.Size--;

                    success = true;
                }
            }

            if (!success) {
                return (false, node);
            }

            this.Balance(node);
            return (true, node);
        }

        private T Min_Core(Node node) {
            var current = node;

            while (current.Left != null) {
                current = current.Left;
            }

            return current.Value;
        }

        private T Max_Core(Node node) {
            var current = node;

            while (current.Right != null) {
                current = current.Right;
            }

            return current.Value;
        }

        private T Indexing_Core(Node node, int index) {
            int left = node.Left?.Size ?? 0;

            if (index == left) {
                return node.Value;
            }
            else if (index < left) {
                return Indexing_Core(node.Left, index);
            }
            else {
                return Indexing_Core(node.Right, index - left - 1);
            }
        }

        private int IndexOf_Core(Node node, T item, int index) {
            if (node == null) {
                return ~index;
            }

            int compare = this.comparer.Compare(item, node.Value);
            if (compare > 0) {
                return this.IndexOf_Core(node.Right, item, index + (node.Left?.Size ?? 0) + 1);
            }
            else if (compare < 0) {
                return this.IndexOf_Core(node.Left, item, index);
            }
            else {
                return index + (node.Left?.Size ?? 0);
            }
        }

        // Note - Balance, LeftRotate, and RightRotate don't need locks because they are only called
        // from methods that are locked
        private void Balance(Node node) {
            // Minimum of 1 for weights
            int rightWeight = (node.Right?.Size ?? 0) + 1;
            int leftWeight = (node.Left?.Size ?? 0) + 1;

            // Left too big (Garaunteed left pivot)
            if (leftWeight > 2.5f * rightWeight) {
                int pivotRightWeight = (node.Left.Right?.Size ?? 0) + 1;
                int pivotLeftWeight = (node.Left.Left?.Size ?? 0) + 1;

                if (pivotRightWeight >= 1.5f * pivotLeftWeight) {
                    // The right child of the left child is too big (Garaunteed pivot)
                    this.LeftRotate(node.Left);
                }

                // The left child of the pivot is too big
                this.RightRotate(node);
            }

            // Right too big (Garaunteed right pivot)
            if (rightWeight > 2.5f * leftWeight) {
                int pivotRightWeight = (node.Right.Right?.Size ?? 0) + 1;
                int pivotLeftWeight = (node.Right.Left?.Size ?? 0) + 1;

                if (pivotLeftWeight >= 1.5f * pivotRightWeight) {
                    // The left child of the right child is too big (Garaunteed pivot)
                    this.RightRotate(node.Right);
                }

                // The right child of beta is too big
                this.LeftRotate(node);
            }
        }

        private void LeftRotate(Node node) {
            var pivot = node.Right;

            // Swap data
            var temp = node.Value;
            node.Value = pivot.Value;
            pivot.Value = temp;

            // Adjust node references
            node.Right = pivot.Right;
            pivot.Right = pivot.Left;
            pivot.Left = node.Left;
            node.Left = pivot;

            // Adjust sizes
            pivot.Size = (pivot.Left?.Size ?? 0) + (pivot.Right?.Size ?? 0) + 1;
            node.Size = pivot.Size + (node.Right?.Size ?? 0) + 1;
        }

        private void RightRotate(Node node) {
            var pivot = node.Left;

            // Swap data
            var temp = node.Value;
            node.Value = pivot.Value;
            pivot.Value = temp;

            // Adjust node references
            node.Left = pivot.Left;
            pivot.Left = pivot.Right;
            pivot.Right = node.Right;
            node.Right = pivot;

            // Adjust sizes
            pivot.Size = (pivot.Left?.Size ?? 0) + (pivot.Right?.Size ?? 0) + 1;
            node.Size = (node.Left?.Size ?? 0) + pivot.Size + 1;
        }

        public IEnumerator<T> GetEnumerator() {
            Stack<Node> nodes = new Stack<Node>();

            int startVersion = this.version;
            var current = this.root;

            while (current != null || nodes.Count > 0) {
                while (current != null) {
                    nodes.Push(current);
                    current = current.Left;
                }

                if (nodes.Count > 0) {
                    var next = nodes.Pop();
                    yield return next.Value;

                    if (this.version != startVersion) {
                        throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                    }

                    current = next.Right;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public void Clear() {

            this.root = null;
        }

        public bool Contains(T item) => this.IndexOf(item) > 0;

        public void CopyTo(T[] array, int arrayIndex) {
            if (array == null) throw new ArgumentNullException(nameof(array));

            foreach (var item in this) {
                array[arrayIndex++] = item;
            }
        }

        void ICollection<T>.Add(T item) => this.Add(item);

        private class Node {
            public T Value { get; set; }

            public Node Right { get; set; }

            public Node Left { get; set; }

            public int Size { get; set; }

            public override string ToString() {
                return $"Value= {this.Value}, Size= {this.Size}";
            }
        }
    }
}