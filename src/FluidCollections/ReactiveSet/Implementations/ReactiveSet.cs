using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public class ReactiveSet<T> : ICollectedReactiveSet<T> {
        private readonly Subject<IEnumerable<ReactiveSetChange<T>>> changes = new Subject<IEnumerable<ReactiveSetChange<T>>>();
        private readonly ISet<T> set;

        public event PropertyChangedEventHandler PropertyChanged;

        private object SyncRoot { get; } = new object();

        public int Count => this.set.Count;

        public ReactiveSet() : this(new HashSet<T>()) { }

        public ReactiveSet(IEnumerable<T> items) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }

            this.set = new HashSet<T>(items);
        }

        public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
            return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                lock (this.SyncRoot) {
                    var initialState = this.set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add)).ToArray();
                    observer.OnNext(initialState);

                    var subscription = this.changes.Subscribe(observer);
                    return () => subscription.Dispose();
                }
            });
        }

        public bool Add(T item) {
            lock (this.SyncRoot) {
                if (!this.set.Contains(item)) {
                    // Update observers
                    this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Add) });

                    this.set.Add(item);
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    return true;
                }
            }

            return false;
        }

        public int AddRange(IEnumerable<T> elements) {
            int added = 0;

            lock (this.SyncRoot) {
                var addedItems = new HashSet<T>();
                var changes = new List<ReactiveSetChange<T>>();

                foreach (var item in elements) {
                    if (!this.set.Contains(item) && !addedItems.Contains(item)) {
                        added++;
                        changes.Add(new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Add));
                        addedItems.Add(item);
                    }
                }

                this.changes.OnNext(changes);

                foreach (T item in addedItems) {
                    this.set.Add(item);
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            }

            return added;
        }

        public bool Remove(T item) {
            lock (this.SyncRoot) {
                if (this.set.Contains(item)) {
                    // Update observers
                    this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Remove) });

                    this.set.Remove(item);
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    return true;
                }
            }

            return false;
        }

        public int RemoveRange(IEnumerable<T> elements) {
            int removed = 0;

            lock (this.SyncRoot) {
                var removedItems = new HashSet<T>();
                var changes = new List<ReactiveSetChange<T>>();

                foreach (var item in elements) {
                    if (this.set.Contains(item) && !removedItems.Contains(item)) {
                        removed++;
                        changes.Add(new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Remove));
                        removedItems.Add(item);
                    }
                }

                this.changes.OnNext(changes);

                foreach (T item in removedItems) {
                    this.set.Add(item);
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            }

            return removed;
        }

        public void Edit(IEnumerable<T> elements) {
            ISet<T> set;

            if (elements is ISet<T> elemSet) {
                set = elemSet;
            }
            else {
                set = new HashSet<T>(elements);
            }

            lock (this.SyncRoot) {
                var toAdd = new List<T>();
                var toRemove = new List<T>();

                foreach (var item in set) {
                    if (!this.Contains(item)) {
                        toAdd.Add(item);
                    }
                }

                foreach (var item in this.set) {
                    if (!set.Contains(item)) {
                        toRemove.Add(item);
                    }
                }

                this.RemoveRange(toRemove);
                this.AddRange(toAdd);
            }
        }

        public void Clear() {
            lock (this.SyncRoot) {
                var state = this.set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Remove)).ToArray();

                // Update observers
                this.changes.OnNext(state);

                this.set.Clear();
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            }
        }

        public void Dispose() {
            this.changes.OnCompleted();
        }

        public IReadOnlyCollection<T> ToCollection() {
            lock (this.SyncRoot) {
                return this.set.ToArray();
            }
        }

        public bool Contains(T value) => this.set.Contains(value);
    }
}