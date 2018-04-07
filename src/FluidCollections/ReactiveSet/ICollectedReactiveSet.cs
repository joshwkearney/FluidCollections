using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FluidCollections {
    public interface ICollectedReactiveSet<T> : IReactiveSet<T>, IDisposable, INotifyPropertyChanged {
        int Count { get; }

        IEnumerable<T> AsEnumerable();
    }
}