using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FluidCollections {
    public interface ICollectedReactiveSet<T> : IReactiveSet<T>, IDisposable, INotifyPropertyChanged {
        int Count { get; }

        IReadOnlyCollection<T> ToCollection();
    }
}