using System;
using System.Collections.Generic;

namespace FluidCollections {
    public interface IReactiveSet<T> {
        IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable();

        bool Contains(T item);
    }
}