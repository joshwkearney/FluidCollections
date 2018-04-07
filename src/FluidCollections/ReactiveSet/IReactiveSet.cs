using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Text;

namespace FluidCollections {
    public interface IReactiveSet<T> {
        IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable();

        bool Contains(T item);
    }
}