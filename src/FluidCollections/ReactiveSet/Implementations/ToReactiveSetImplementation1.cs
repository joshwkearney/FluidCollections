using System;
using System.Collections.Generic;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        internal class ToReactiveSetImplementation<T> : IReactiveSet<T> {
            private readonly IObservable<ReactiveSetChange<T>> inner;
            private readonly Func<T, bool> contains;

            public ToReactiveSetImplementation(IObservable<ReactiveSetChange<T>> inner, Func<T, bool> contains) {
                this.inner = inner;
                this.contains = contains;
            }

            public IObservable<ReactiveSetChange<T>> AsObservable() {
                return this.inner;
            }

            public bool Contains(T item) => this.contains(item);

            public void Dispose() { }
        }
    }
}