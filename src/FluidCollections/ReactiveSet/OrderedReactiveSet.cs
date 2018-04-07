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
    public class OrderedReactiveSet<T> : IOrderedReactiveSet<T> {
        private readonly Subject<IEnumerable<ReactiveSetChange<T>>> changes = new Subject<IEnumerable<ReactiveSetChange<T>>>();
        private readonly object lockObj = new object();
        private readonly OrderedSet<T> set;

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => this.set.Count;

        public T Min => this.set.Min;

        public T Max => this.set.Max;

        public T this[int index] => this.set[index];

        public OrderedReactiveSet() : this(Comparer<T>.Default) { }

        public OrderedReactiveSet(IComparer<T> comparer) {
            if (comparer == null) {
                throw new ArgumentNullException(nameof(comparer));
            }

            this.set = new OrderedSet<T>(comparer);
        }

        public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
            return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                lock (this.lockObj) {
                    var initialState = this.set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add)).ToArray();

                    if (initialState.Any()) {
                        observer.OnNext(initialState);
                    }

                    var subscription = this.changes.Subscribe(observer);
                    return () => subscription.Dispose();
                }
            });
        }

        public bool Add(T item) {
            lock (this.lockObj) {
                if (!this.set.Contains(item)) {
                    this.set.Add(item);

                    // Update observers
                    this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Add) });

                    this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, this.set.IndexOf(item)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));

                    return true;
                }
            }

            return false;
        }

        public bool Remove(T item) {
            lock (this.lockObj) {
                if (this.set.Contains(item)) {
                    this.set.Remove(item);

                    // Update observers
                    this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Remove) });

                    this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, this.set.IndexOf(item)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));

                    return true;
                }
            }

            return false;
        }

        public bool RemoveAt(int index) => this.set.RemoveAt(index);

        public int IndexOf(T item) => this.set.IndexOf(item);

        public void Clear() {
            lock (this.lockObj) {
                var state = this.set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Remove)).ToArray();

                this.set.Clear();

                // Update observers
                this.changes.OnNext(state);

                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));
            }
        }

        public void Dispose() {
            this.changes.OnCompleted();
        }

        public IEnumerable<T> AsEnumerable() => this.set;

        public bool Contains(T value) => this.set.Contains(value);

        public void CopyTo(T[] array, int startIndex) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }

            lock (this.lockObj) {
                this.set.CopyTo(array, startIndex);
            }
        }
    }
}