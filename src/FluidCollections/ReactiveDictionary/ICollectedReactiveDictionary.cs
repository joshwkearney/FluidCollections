using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FluidCollections {
    public interface ICollectedReactiveDictionary<TKey, TValue> : IReactiveDictionary<TKey, TValue>, IDisposable, INotifyPropertyChanged {
        int Count { get; }

        object SyncRoot { get; }

        IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable();
    }
}