using System;
using System.Collections.Generic;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        internal class ToReactiveSetImplementation1<T> : IReactiveSet<T> {
            private readonly IObservable<IEnumerable<ReactiveSetChange<T>>> inner;
            private readonly Func<T, bool> contains;

            public ToReactiveSetImplementation1(IObservable<IEnumerable<ReactiveSetChange<T>>> inner, Func<T, bool> contains) {
                this.inner = inner;
                this.contains = contains;
            }

            public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
                return this.inner;
            }

            public bool Contains(T item) => this.contains(item);

            public void Dispose() { }
        }
    }
}