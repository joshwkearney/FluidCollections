using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    internal class SetSelectImplementation<T, TResult> : ICollectedReactiveSet<TResult> {
        private readonly Subject<IEnumerable<ReactiveSetChange<TResult>>> changes = new Subject<IEnumerable<ReactiveSetChange<TResult>>>();
        private readonly IDisposable subscriptions;
        private readonly Func<T, TResult> selector;

        private readonly IDictionary<TResult, int> counts;
        private readonly Dictionary<T, TResult> conversions = new Dictionary<T, TResult>();

        public event PropertyChangedEventHandler PropertyChanged;

        public object SyncRoot { get; } = new object();

        public int Count => this.counts.Count;

        public SetSelectImplementation(IObservable<IEnumerable<ReactiveSetChange<T>>> inner, Func<T, TResult> selector, IDictionary<TResult, int> dict) {
            this.selector = selector;

            this.counts = dict;
            this.counts.Clear();

            this.subscriptions = inner.Subscribe(
                this.ProcessIncomingChanges,
                this.changes.OnError,
                this.changes.OnCompleted
            );
        }

        public IReadOnlyCollection<TResult> ToCollection() {
            lock (this.SyncRoot) {
                return this.counts.Keys.ToArray();
            }
        }

        public IObservable<IEnumerable<ReactiveSetChange<TResult>>> AsObservable() {
            return Observable.Create<IEnumerable<ReactiveSetChange<TResult>>>(observer => {
                lock (this.SyncRoot) {
                    var initialState = this.counts.Select(x => new ReactiveSetChange<TResult>(x.Key, ReactiveSetChangeReason.Add)).ToArray();
                    observer.OnNext(initialState);

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
            lock (this.SyncRoot) {
                // Compute new changes
                foreach (var change in changes) {
                    if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                        if (!this.conversions.ContainsKey(change.Value)) {
                            var selected = this.selector(change.Value);
                            this.conversions[change.Value] = selected;
                            var newChange = new ReactiveSetChange<TResult>(selected, change.ChangeReason);

                            if (!this.counts.ContainsKey(newChange.Value)) {
                                updates.Add(newChange);
                            }
                        }
                    }
                    else if (this.conversions.TryGetValue(change.Value, out var newValue)) {
                        var newChange = new ReactiveSetChange<TResult>(newValue, change.ChangeReason);

                        // Remove the item and maybe remove the dictionary entry
                        if (this.counts.ContainsKey(newChange.Value)) {
                            this.conversions.Remove(change.Value);
                            updates.Add(newChange);
                        }
                    }
                }

                // Signal observers of the change
                this.changes.OnNext(updates);
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                // Update internal set
                foreach (var change in updates) {
                    if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                        if (!this.counts.ContainsKey(change.Value)) {
                            this.counts[change.Value] = 0;
                        }

                        this.counts[change.Value]++;
                    }
                    else {
                        this.counts[change.Value]--;

                        if (this.counts[change.Value] == 0) {
                            this.counts.Remove(change.Value);
                        }
                    }
                }
            }
        }
    }
}