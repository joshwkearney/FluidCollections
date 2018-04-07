using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {       
        public static IOrderedReactiveSet<T> OrderBy<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return new OrderedReactiveSet<T>(set);
        }

        public static IOrderedReactiveSet<T> OrderBy<T, TCompare>(this IReactiveSet<T> set, Func<T, TCompare> orderBy) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));

            var comparer = Comparer<TCompare>.Default;

            return new OrderedReactiveSet<T>(set, Comparer<T>.Create(
                 (x, y) => comparer.Compare(orderBy(x), orderBy(y))
            ));
        }

        private class OrderedReactiveSet<T> : IOrderedReactiveSet<T> {
            private readonly object lockObj = new object();
            private readonly OrderedSet<T> list;
            private readonly Subject<IEnumerable<ReactiveSetChange<T>>> subject = new Subject<IEnumerable<ReactiveSetChange<T>>>();
            private readonly IDisposable subscriptions;

            public event NotifyCollectionChangedEventHandler CollectionChanged;
            public event PropertyChangedEventHandler PropertyChanged;

            public int Count => this.list.Count;

            public T this[int index] => this.list[index];

            public T Min => this.list.Min;

            public T Max => this.list.Max;

            public IEnumerable<T> AsEnumerable() => this.list;

            public OrderedReactiveSet(IReactiveSet<T> inner) : this(inner, Comparer<T>.Default) { }

            public OrderedReactiveSet(IReactiveSet<T> inner, IComparer<T> comparer) {
                this.list = new OrderedSet<T>(comparer);

                this.subscriptions = inner.AsObservable().Subscribe(
                    ProcessIncomingChanges,
                    this.subject.OnError,
                    this.subject.OnCompleted
                );
            }

            public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
                return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                    lock (this.lockObj) {
                        var changes = this.list.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add));

                        if (changes.Any()) {
                            observer.OnNext(changes);
                        }

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
                lock (lockObj) {
                    foreach (var change in changes) {                        
                        if (change.ChangeReason == ReactiveSetChangeReason.Add && this.list.Add(change.Value)) {
                            this.CollectionChanged?.Invoke(
                                this,
                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, change.Value, list.IndexOf(change.Value))
                            );
                        }
                        else {
                            if (this.list.Remove(change.Value)) {
                                this.CollectionChanged?.Invoke(
                                    this,
                                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, change.Value, this.list.IndexOf(change.Value))
                                );
                            }
                        }
                    }
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));

                // Signal observers of the change
                this.subject.OnNext(changes);
            }

            public int IndexOf(T item) => this.list.IndexOf(item);
        }
    }
}