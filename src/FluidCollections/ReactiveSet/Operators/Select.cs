using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static ICollectedReactiveSet<TResult> Select<T, TResult>(this IReactiveSet<T> set, Func<T, TResult> filter) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return new SelectImplementation<T, TResult>(set, filter);
        }
        
        private class SelectImplementation<T, TResult> : ICollectedReactiveSet<TResult> {
            private readonly Subject<IEnumerable<ReactiveSetChange<TResult>>> changes = new Subject<IEnumerable<ReactiveSetChange<TResult>>>();
            private readonly object lockObj = new object();
            private readonly IDisposable subscriptions;
            private readonly Dictionary<TResult, int> counts = new Dictionary<TResult, int>();
            private readonly Func<T, TResult> selector;
            private readonly IReactiveSet<T> inner;

            public event PropertyChangedEventHandler PropertyChanged;

            public int Count => this.counts.Count;

            public SelectImplementation(IReactiveSet<T> inner, Func<T, TResult> selector) {
                this.selector = selector;
                this.inner = inner;

                this.subscriptions = inner.AsObservable().Subscribe(
                    this.ProcessIncomingChanges,
                    this.changes.OnError,
                    this.changes.OnCompleted
                );
            }

            public IEnumerable<TResult> AsEnumerable() => this.counts.Keys;

            public IObservable<IEnumerable<ReactiveSetChange<TResult>>> AsObservable() {
                return Observable.Create<IEnumerable<ReactiveSetChange<TResult>>>(observer => {
                    lock (this.lockObj) {
                        var initialState = this.counts.Select(x => new ReactiveSetChange<TResult>(x.Key, ReactiveSetChangeReason.Add)).ToArray();

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

            public bool Contains(TResult value) => this.counts.ContainsKey(value);

            private void ProcessIncomingChanges(IEnumerable<ReactiveSetChange<T>> changes) {
                var updates = new List<ReactiveSetChange<TResult>>();

                // Update the local set first
                lock (lockObj) { 
                    foreach (var change in changes) {
                        var newChange = new ReactiveSetChange<TResult>(this.selector(change.Value), change.ChangeReason);

                        if (newChange.ChangeReason == ReactiveSetChangeReason.Add) {
                            if (this.counts.ContainsKey(newChange.Value)) {
                                this.counts[newChange.Value]++;
                            }
                            else {
                                this.counts[newChange.Value] = 1;
                                updates.Add(newChange);
                            }
                        }
                        else {
                            // Remove the item and maybe remove the dictionary entry
                            if (this.counts.TryGetValue(newChange.Value, out int count)) {                                
                                if (count == 1) {
                                    this.counts.Remove(newChange.Value);
                                    updates.Add(newChange);
                                }
                                else {
                                    this.counts[newChange.Value]--;
                                }
                            }
                        }
                    }
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                // Signal observers of the change
                this.changes.OnNext(updates);
            }
        }
    }
}