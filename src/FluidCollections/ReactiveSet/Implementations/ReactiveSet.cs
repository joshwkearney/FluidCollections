using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public class ReactiveSet<T> : ICollectedReactiveSet<T> {
        private readonly Subject<ReactiveSetChange<T>> changes = new Subject<ReactiveSetChange<T>>();
        private readonly ISet<T> set;
        private readonly object syncRoot = new object();
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => this.set.Count;

        public ReactiveSet() : this(new T[0]) { }

        public ReactiveSet(IEnumerable<T> items) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }

            this.set = new HashSet<T>(items);
        }

        public ReactiveSet(IObservable<ReactiveSetChange<T>> changeStream) {
            if (changeStream == null) {
                throw new ArgumentNullException(nameof(changeStream));
            }

            this.set = new HashSet<T>();

            this.subscriptions.Add(
                changeStream.Subscribe(
                    change => {
                        if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                            this.AddRange(change.Items);
                        }
                        else {
                            this.RemoveRange(change.Items);
                        }
                    },
                    this.changes.OnError,
                    this.changes.OnCompleted
                )
            );
        }

        public bool Contains(T value) => this.set.Contains(value);

        public IObservable<ReactiveSetChange<T>> AsObservable() {
            return Observable.Create<ReactiveSetChange<T>>(observer => {
                lock (this.syncRoot) {
                    var initialState = new ReactiveSetChange<T>(ReactiveSetChangeReason.Add, this.set);
                    observer.OnNext(initialState);

                    return this.changes.Subscribe(observer);
                }
            });
        }

        public bool Add(T item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            if (!this.set.Contains(item)) {
                lock (this.syncRoot) {
                    // Update observers
                    var change = new ReactiveSetChange<T>(ReactiveSetChangeReason.Add, new[] { item });
                    this.changes.OnNext(change);

                    this.set.Add(item);
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    return true;
                }
            }

            return false;
        }

        public int AddRange(IEnumerable<T> elements) {
            if (elements == null) {
                throw new ArgumentNullException(nameof(elements));
            }

            lock (this.syncRoot) {
                // Figure out which items need to be added
                var addedItems = new HashSet<T>();
                foreach (var item in elements) {
                    if (!this.set.Contains(item)) {
                        addedItems.Add(item);
                    }
                }

                // Produce a change
                var change = new ReactiveSetChange<T>(ReactiveSetChangeReason.Add, addedItems);
                this.changes.OnNext(change);

                // Update the set
                foreach (T item in addedItems) {
                    this.set.Add(item);
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                return addedItems.Count;
            }
        }

        public bool Remove(T item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            if (this.set.Contains(item)) {
                lock (this.syncRoot) {
                    // Update observers
                    var change = new ReactiveSetChange<T>(ReactiveSetChangeReason.Remove, new[] { item });
                    this.changes.OnNext(change);

                    // Update the set
                    this.set.Remove(item);
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    return true;
                }
            }

            return false;
        }

        public int RemoveRange(IEnumerable<T> elements) {
            if (elements == null) {
                throw new ArgumentNullException(nameof(elements));
            }

            lock (this.syncRoot) {
                var removedItems = new HashSet<T>();

                foreach (var item in elements) {
                    if (this.set.Contains(item)) {
                        removedItems.Add(item);
                    }
                }

                var change = new ReactiveSetChange<T>(ReactiveSetChangeReason.Remove, removedItems);
                this.changes.OnNext(change);

                foreach (T item in removedItems) {
                    this.set.Remove(item);
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                return removedItems.Count;
            }
        }

        public void EditTo(IEnumerable<T> elements) {
            if (elements == null) {
                throw new ArgumentNullException(nameof(elements));
            }

            ISet<T> set;
            if (elements is ISet<T> elemSet) {
                set = elemSet;
            }
            else {
                set = new HashSet<T>(elements);
            }

            lock (this.syncRoot) {
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
            lock (this.syncRoot) {
                // Update observers
                var change = new ReactiveSetChange<T>(ReactiveSetChangeReason.Remove, this.set);
                this.changes.OnNext(change);

                this.set.Clear();
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            }
        }

        public void Dispose() {
            foreach (var sub in this.subscriptions) {
                sub.Dispose();
            }

            this.changes.OnCompleted();
        }
    }
}