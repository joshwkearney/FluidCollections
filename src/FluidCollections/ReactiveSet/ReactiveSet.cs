using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public class ReactiveSet<T> : ICollectedReactiveSet<T> {
        private readonly Subject<IEnumerable<ReactiveSetChange<T>>> changes = new Subject<IEnumerable<ReactiveSetChange<T>>>();
        private readonly object lockObj = new object();
        private readonly ISet<T> set;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => this.set.Count;

        public ReactiveSet() : this(new HashSet<T>()) { }

        public ReactiveSet(ISet<T> innerSet) {
            this.set = innerSet ?? throw new ArgumentNullException(nameof(innerSet));
        }

        public IObservable<IEnumerable<ReactiveSetChange<T>>> AsObservable() {
            return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                lock (this.lockObj) {
                    var initialState = this.set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Add)).ToArray();

                    if (initialState.Any()) {
                        observer.OnNext(initialState);
                    }

                    var subscription = this.changes.Subscribe(observer);
                    return () => subscription.Dispose();
                }
            });
        }

        public bool Add(T item) {
            lock (this.lockObj) {
                if (!this.set.Contains(item)) {
                    this.set.Add(item);

                    // Update observers
                    this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Add) });
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    return true;
                }
            }

            return false;
        }

        public bool Remove(T item) {
            lock (this.lockObj) {
                if (this.set.Contains(item)) {
                    this.set.Remove(item);

                    // Update observers
                    this.changes.OnNext(new[] { new ReactiveSetChange<T>(item, ReactiveSetChangeReason.Remove) });
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    return true;
                }
            }

            return false;
        }

        public void Clear() {
            lock (this.lockObj) {
                var state = this.set.Select(x => new ReactiveSetChange<T>(x, ReactiveSetChangeReason.Remove)).ToArray();

                this.set.Clear();

                // Update observers
                this.changes.OnNext(state);

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            }
        }

        public void Dispose() {
            this.changes.OnCompleted();
        }

        public IEnumerable<T> AsEnumerable() => this.set;

        public bool Contains(T value) => this.set.Contains(value);

        public void CopyTo(T[] array, int startIndex) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }

            lock (this.lockObj) {
                this.set.CopyTo(array, startIndex);
            }
        }
    }
}