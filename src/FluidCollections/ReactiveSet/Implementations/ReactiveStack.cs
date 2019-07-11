using FluidCollections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Specialized;

namespace FluidCollections {
    public class ReactiveStack<T> : IOrderedReactiveSet<T> {
        private readonly List<T> items;
        private readonly ObservableCollectionImpl<T> observable;

        private readonly Subject<IEnumerable<ReactiveSetChange<T>>> changes =
            new Subject<IEnumerable<ReactiveSetChange<T>>>();

        public event PropertyChangedEventHandler PropertyChanged;

        public T this[int index] => this.items[index];

        public T Min => this.items[0];

        public T Max => this.items[this.items.Count - 1];

        public int Count => this.items.Count;

        public object SyncRoot { get; } = new object();

        public ReactiveStack() {
            this.items = new List<T>();
            this.observable = new ObservableCollectionImpl<T>(this.items);
        }

        public ReactiveStack(IEnumerable<T> items) {
            this.items = new List<T>(items);
            this.observable = new ObservableCollectionImpl<T>(this.items);
        }

        public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
            return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                lock (this.SyncRoot) {
                    var initialState = this.items.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add)).ToArray();
                    observer.OnNext(initialState);

                    var subscription = this.changes.Subscribe(observer);
                    return () => subscription.Dispose();
                }
            });
        }

        public bool Contains(T item) => this.items.Contains(item);

        public void Dispose() => this.changes.OnCompleted();

        public int IndexOf(T item) => this.items.IndexOf(item);

        public bool Push(T item) {
            if (this.Contains(item)) {
                return false;
            }

            lock (this.SyncRoot) {
                // Update observers
                this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Add) });

                // Update internal collection
                this.items.Add(item);

                // Update change events and such
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));

                if (this.Count == 0) {
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                }

                // Update observable collection
                this.observable.ChangeCollection(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, this.observable.Count - 1)
                );
            }

            return true;
        }

        public T Pop() {
            if (this.Count == 0) {
                throw new InvalidOperationException("Cannot pop, collection is empty");
            }

            lock (this.SyncRoot) {
                T item = this.Max;

                // Update observers
                this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Remove) });

                // Update internal collection
                this.items.RemoveAt(this.items.Count - 1);

                // Update events and such
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));

                if (this.Count == 1) {
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                }

                // Update observable collection
                this.observable.ChangeCollection(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, this.observable.Count)
                );

                return item;
            }
        }

        public void Clear() {
            lock (this.SyncRoot) {
                var changes = this.items.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Remove)).ToArray();

                // Update observers
                this.changes.OnNext(changes);

                // Update list
                this.items.Clear();

                /// Update events and such
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));

                // Update observable collection
                this.observable.ChangeCollection(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
                );
            }
        }

        public IObservableCollection<T> ToObservableCollection() => this.observable;

        public IReadOnlyCollection<T> ToCollection() {
            lock (this.SyncRoot) {
                return this.items.ToArray();
            }
        }
    }
}