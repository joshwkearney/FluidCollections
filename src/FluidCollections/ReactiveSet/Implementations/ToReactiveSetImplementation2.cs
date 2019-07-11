using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        internal class ToReactiveSetImplementation2<T> : ICollectedReactiveSet<T> {
            private readonly Subject<IEnumerable<ReactiveSetChange<T>>> changes = new Subject<IEnumerable<ReactiveSetChange<T>>>();
            private readonly IDisposable subscriptions;

            protected ISet<T> Set { get; }

            public event PropertyChangedEventHandler PropertyChanged;

            public int Count => this.Set.Count;

            public object SyncRoot { get; } = new object();

            public ToReactiveSetImplementation2(ISet<T> set, IObservable<IEnumerable<ReactiveSetChange<T>>> changesObservable) {
                this.Set = set;

                this.subscriptions = changesObservable.Subscribe(
                    this.ProcessIncomingChanges,
                    this.changes.OnError,
                    this.changes.OnCompleted
                );
            }

            public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
                return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                    lock (this.SyncRoot) {
                        var initialState = this.Set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add)).ToArray();
                        observer.OnNext(initialState);

                        return this.changes.Subscribe(observer);
                    }
                });
            }

            public void Dispose() {
                this.changes.OnCompleted();
                this.subscriptions.Dispose();
            }

            public IReadOnlyCollection<T> ToCollection() {
                lock (this.SyncRoot) {
                    return this.Set.ToArray();
                }
            }

            public bool Contains(T value) => this.Set.Contains(value);

            protected virtual void ProcessIncomingChanges(IEnumerable<ReactiveSetChange<T>> changes) {
                // Update the local set first
                lock (SyncRoot) {
                    // Signal observers of the change
                    this.changes.OnNext(changes);

                    foreach (var change in changes) {
                        if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                            this.Set.Add(change.Value);
                        }
                        else {
                            this.Set.Remove(change.Value);
                        }
                    }

                    this.RaisePropertyChanged(nameof(this.Count));
                }
            }

            protected void RaisePropertyChanged(string name) {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}