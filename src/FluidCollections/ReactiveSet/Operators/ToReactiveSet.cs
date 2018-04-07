using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static ICollectedReactiveSet<T> ToReactiveSet<T>(this IObservable<IEnumerable<ReactiveSetChange<T>>> observable) {
            if (observable == null) throw new ArgumentNullException(nameof(observable));

            return new DependentReactiveSet<T>(new HashSet<T>(), observable);
        }

        internal static IReactiveSet<T> ToReactiveSet<T>(this IObservable<IEnumerable<ReactiveSetChange<T>>> observable, Func<T, bool> containsFunc) {
            if (observable == null) throw new ArgumentNullException(nameof(observable));
            if (containsFunc == null) throw new ArgumentNullException(nameof(containsFunc));

            return new AsReactiveSetImplementation<T>(observable, containsFunc);
        }

        public static ICollectedReactiveSet<T> ToReactiveSet<T>(this IReactiveSet<T> set, ISet<T> innerSet) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (innerSet == null) throw new ArgumentNullException(nameof(innerSet));

            return new DependentReactiveSet<T>(innerSet, set.AsObservable());
        }

        public static ICollectedReactiveSet<T> ToReactiveSet<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.ToReactiveSet(new HashSet<T>());
        }

        public static ICollectedReactiveSet<T> ToConcurrentReactiveSet<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.ToReactiveSet(new ConcurrentSet<T>());
        }

        private class AsReactiveSetImplementation<T> : IReactiveSet<T> {
            private readonly IObservable<IEnumerable<ReactiveSetChange<T>>> inner;
            private readonly Func<T, bool> contains;

            public AsReactiveSetImplementation(IObservable<IEnumerable<ReactiveSetChange<T>>> inner, Func<T, bool> contains) {
                this.inner = inner;
                this.contains = contains;
            }

            public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
                return this.inner;
            }

            public bool Contains(T item) => this.contains(item);

            public void Dispose() { }
        }

        private class DependentReactiveSet<T> : ICollectedReactiveSet<T> {
            private readonly Subject<IEnumerable<ReactiveSetChange<T>>> changes = new Subject<IEnumerable<ReactiveSetChange<T>>>();
            private readonly object lockObj = new object();
            private readonly IDisposable subscriptions;

            protected ISet<T> Set { get; }

            public event PropertyChangedEventHandler PropertyChanged;

            public int Count => this.Set.Count;

            public DependentReactiveSet(ISet<T> set, IObservable<IEnumerable<ReactiveSetChange<T>>> changesObservable) {
                this.Set = set;

                this.subscriptions = changesObservable.Subscribe(
                    this.ProcessIncomingChanges,
                    this.changes.OnError,
                    this.changes.OnCompleted
                );
            }

            public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
                return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                    lock (this.lockObj) {
                        var initialState = this.Set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add)).ToArray();

                        if (initialState.Any()) {
                            observer.OnNext(initialState);
                        }

                        return this.changes.Subscribe(observer);
                    }
                });
            }

            public void Dispose() {
                this.changes.OnCompleted();
                this.subscriptions.Dispose();
            }

            public IEnumerable<T> AsEnumerable() => this.Set;

            public bool Contains(T value) => this.Set.Contains(value);

            protected virtual void ProcessIncomingChanges(IEnumerable<ReactiveSetChange<T>> changes) {
                // Update the local set first
                lock (lockObj) {
                    foreach (var change in changes) {
                        if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                            this.Set.Add(change.Value);
                        }
                        else if (change.ChangeReason == ReactiveSetChangeReason.Remove) {
                            this.Set.Remove(change.Value);
                        }
                        else {
                            throw new InvalidOperationException();
                        }
                    }
                }

                this.RaisePropertyChanged(nameof(this.Count));

                // Signal observers of the change
                this.changes.OnNext(changes);
            }

            protected void RaisePropertyChanged(string name) {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}