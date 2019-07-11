using System;

namespace FluidCollections {
    public enum ReactiveSetChangeReason {
        Add = 0,
        Remove = 1
    }

    public struct ReactiveSetChange<T> : IEquatable<ReactiveSetChange<T>> {
        public ReactiveSetChangeReason ChangeReason { get; }

        public T Value { get; }

        public ReactiveSetChange(T item, ReactiveSetChangeReason reason) {
            this.Value = item;
            this.ChangeReason = reason;
        }

        public bool Equals(ReactiveSetChange<T> other) {
            return this.ChangeReason == other.ChangeReason && this.Value.Equals(other.Value);
        }

        public override bool Equals(object obj) {
            if (obj is ReactiveSetChange<T> change) {
                return this.Equals(change);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.ChangeReason.GetHashCode() + 37 * (this.Value?.GetHashCode() ?? 1);
        }
    }
}