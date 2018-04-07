using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace FluidCollections {
    public interface IOrderedReactiveSet<T> : ICollectedReactiveSet<T>, INotifyCollectionChanged {
        T this[int index] { get; }

        T Min { get; }

        T Max { get; }

        int IndexOf(T item);
    }
}