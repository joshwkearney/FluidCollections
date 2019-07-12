using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    internal class SetSelectImplementation<T, TResult> : ICollectedReactiveSet<TResult> {
        private readonly Subject<ReactiveSetChange<TResult>> changes = new Subject<ReactiveSetChange<TResult>>();
        private readonly IDisposable subscriptions;
        private readonly Func<T, TResult> selector;

        private readonly Dictionary<TResult, int> counts = new Dictionary<TResult, int>();
        private readonly Dictionary<T, TResult> conversions = new Dictionary<T, TResult>();

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly object syncRoot = new object();

        public int Count => this.counts.Count;

        public SetSelectImplementation(IObservable<ReactiveSetChange<T>> inner, Func<T, TResult> selector) {
            this.selector = selector;

            this.subscriptions = inner.Subscribe(
                this.ProcessIncomingChanges,
                this.changes.OnError,
                this.changes.OnCompleted
            );
        }

        public IObservable<ReactiveSetChange<TResult>> AsObservable() {
            return Observable.Create<ReactiveSetChange<TResult>>(observer => {
                lock (this.syncRoot) {
                    var change = new ReactiveSetChange<TResult>(ReactiveSetChangeReason.Add, this.counts.Keys);
                    observer.OnNext(change);

                    return this.changes.Subscribe(observer);
                }
            });
        }

        public void Dispose() {
            this.changes.OnCompleted();
            this.subscriptions.Dispose();
        }

        public bool Contains(TResult value) => this.counts.ContainsKey(value);

        private void ProcessIncomingChanges(ReactiveSetChange<T> change) {
            // Update the local set first
            lock (this.syncRoot) {
                ReactiveSetChange<TResult> newChange;

                // Compute new changes
                if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                    var addedItems = new List<TResult>();

                    foreach (var item in change.Items) {
                        // We haven't uncountered this item yet
                        if (!this.conversions.ContainsKey(item)) {
                            // Convert and store the conversion
                            var selected = this.selector(item);
                            this.conversions[item] = selected;

                            // If we haven't encountered this output before, make sure to produce a change
                            // notification and add it to the counts
                            if (!this.counts.ContainsKey(selected)) {
                                addedItems.Add(selected);
                                this.counts[selected] = 0;
                            }

                            this.counts[selected]++;
                        }
                    }

                    newChange = new ReactiveSetChange<TResult>(ReactiveSetChangeReason.Add, addedItems);
                }
                else {
                    var removedItems = new List<TResult>();

                    foreach (var item in change.Items) {
                        // We can remove this item
                        if (this.conversions.TryGetValue(item, out var newValue)) {
                            // Remove the item
                            this.conversions.Remove(item);

                            // Decrement the count of this output
                            this.counts[newValue]--;

                            // Produce a change notification if we ran out of this output
                            if (this.counts[newValue] == 0) {
                                removedItems.Add(newValue);
                                this.counts.Remove(newValue);
                            }
                        }
                    }

                    newChange = new ReactiveSetChange<TResult>(ReactiveSetChangeReason.Remove, removedItems);
                }

                // Signal observers of the change
                this.changes.OnNext(newChange);
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            }
        }
    }
}