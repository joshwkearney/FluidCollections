using System;
using System.Collections.Generic;

namespace FluidCollections {
    public enum ReactiveSetChangeReason {
        Add = 0,
        Remove = 1
    }

    public struct ReactiveSetChange<T> {
        public ReactiveSetChangeReason ChangeReason { get; }

        public IEnumerable<T> Items { get; }

        public ReactiveSetChange(ReactiveSetChangeReason reason, IEnumerable<T> items) {
            this.Items = items;
            this.ChangeReason = reason;
        }
    }
}