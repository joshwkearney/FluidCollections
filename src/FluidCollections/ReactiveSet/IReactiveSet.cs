using System;
using System.Collections.Generic;

namespace FluidCollections {
    public interface IReactiveSet<T> {
        IObservable<ReactiveSetChange<T>> AsObservable();

        bool Contains(T item);
    }
}