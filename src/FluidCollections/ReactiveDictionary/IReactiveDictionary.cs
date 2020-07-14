using System;
using System.Collections.Generic;

namespace FluidCollections {
    public interface IReactiveDictionary<TKey, TValue> {
        IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> AsObservable();

        TValue this[TKey key] { get; }

        bool TryGetValue(TKey key, out TValue value);
    }
}