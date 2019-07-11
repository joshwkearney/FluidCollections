using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    internal class SetOrderByImplementation<T> : IOrderedReactiveSet<T> {
        private readonly OrderedSet<T> list;
        private readonly ObservableCollectionImpl<T> items;
        private readonly Subject<IEnumerable<ReactiveSetChange<T>>> subject = new Subject<IEnumerable<ReactiveSetChange<T>>>();
        private readonly IDisposable subscriptions;

        public event PropertyChangedEventHandler PropertyChanged;

        public object SyncRoot { get; } = new object();

        public int Count => this.list.Count;

        public T this[int index] => this.list[index];

        public T Min => this.list.Min;

        public T Max => this.list.Max;

        public SetOrderByImplementation(IReactiveSet<T> inner) : this(inner, Comparer<T>.Default) { }

        public SetOrderByImplementation(IReactiveSet<T> inner, IComparer<T> comparer) {
            this.list = new OrderedSet<T>(comparer);
            this.items = new ObservableCollectionImpl<T>(this.list);

            this.subscriptions = inner.AsObservable().Subscribe(
                ProcessIncomingChanges,
                this.subject.OnError,
                this.subject.OnCompleted
            );
        }

        public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
            return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                lock (this.SyncRoot) {
                    var changes = this.list.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add));
                    observer.OnNext(changes);

                    return this.subject.Subscribe(observer);
                }
            });
        }

        public bool Contains(T item) => this.list.Contains(item);

        public void Dispose() {
            this.subscriptions.Dispose();
        }

        private void ProcessIncomingChanges(IEnumerable<ReactiveSetChange<T>> changes) {
            // Update the local set first
            lock (this.SyncRoot) {
                // Signal observers of the change
                this.subject.OnNext(changes);

                foreach (var change in changes) {
                    if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                        var index = list.IndexOf(change.Value);

                        if (this.list.Add(change.Value)) {
                            this.items.ChangeCollection(
                                this.items,
                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, change.Value, list.IndexOf(change.Value))
                            );
                        }
                    }
                    else {
                        int index = this.list.IndexOf(change.Value);

                        if (this.list.Remove(change.Value)) {
                            this.items.ChangeCollection(
                                this.items,
                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, change.Value, index)
                            );
                        }
                    }
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));
            }
        }

        public int IndexOf(T item) => this.list.IndexOf(item);

        public IObservableCollection<T> ToObservableCollection() => this.items;

        public IReadOnlyCollection<T> ToCollection() {
            lock (this.SyncRoot) {
                return this.list.ToArray();
            }
        }
    }
}