using System.Collections.Generic;
using System.Collections.Specialized;

namespace FluidCollections {
    public interface IObservableCollection<T> : IEnumerable<T>, IReadOnlyCollection<T>, INotifyCollectionChanged {
    }
}