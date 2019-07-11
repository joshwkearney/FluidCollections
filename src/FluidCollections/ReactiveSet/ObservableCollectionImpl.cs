using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FluidCollections {
    internal class ObservableCollectionImpl<T> : IObservableCollection<T> {
        private IReadOnlyCollection<T> Items { get; }

        public int Count => this.Items.Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IEnumerator<T> GetEnumerator() => this.Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.Items.GetEnumerator();

        public ObservableCollectionImpl(IReadOnlyCollection<T> items) {
            this.Items = items;
        }

        public void ChangeCollection(object sender, NotifyCollectionChangedEventArgs args) {
            this.CollectionChanged?.Invoke(sender, args);
        }
    }
}